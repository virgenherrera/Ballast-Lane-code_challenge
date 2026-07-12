using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.API.Contracts;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.IntegrationTests.Common;

namespace TaskFlow.IntegrationTests.Auth;

/// <summary>
/// Proves JWT authentication/authorization is actually enforced on
/// [Authorize]-protected task endpoints (EP02-B5-01) and that ownership
/// isolation (AC-003.4) holds across two distinct authenticated principals.
/// Uses the plain (unauthenticated) <see cref="IntegrationTestBase.Client"/>
/// plus hand-generated tokens via <see cref="IntegrationTestBase.Factory"/>
/// rather than <see cref="IntegrationTestBase.AuthenticatedClient"/>, since
/// every test here deliberately varies or omits the Authorization header.
/// </summary>
public sealed class TaskAuthorizationTests : IntegrationTestBase
{
    private const string Endpoint = "/api/tasks";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // AC-003.2
    [Fact]
    public async Task Tasks_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AC-003.3
    [Fact]
    public async Task Tasks_WithExpiredToken_Returns401()
    {
        var expiredToken = Factory.GenerateExpiredTestToken(SeedIdentity.SeedOwnerId);

        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AC-003.4: user isolation — a token for a DIFFERENT owner must never see
    // tasks created under SeedOwnerId (or vice versa).
    [Fact]
    public async Task Tasks_WithOtherUserToken_ReturnsEmpty()
    {
        // Seed a task owned by the default AuthenticatedClient principal
        // (SeedOwnerId) so there IS data in the table to isolate against —
        // an empty table would make this assertion vacuous.
        var createResponse = await AuthenticatedClient.PostAsJsonAsync(
            Endpoint,
            new { title = "Belongs to SeedOwnerId" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var otherUserToken = Factory.GenerateTestToken(
            SeedIdentity.SeedOwnerId2,
            SeedIdentity.SeedOwnerId2Email,
            SeedIdentity.SeedOwnerId2Name);

        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherUserToken);

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskListResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(0, body.Paging.Total);
    }
}
