using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.API.Contracts;
using TaskFlow.IntegrationTests.Common;

namespace TaskFlow.IntegrationTests.Auth;

/// <summary>
/// Full integration suite for POST /api/auth/register, run against a real
/// PostgreSQL Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no
/// InMemory/SQLite providers). Proves every AC-001.x acceptance criterion
/// end-to-end through the real HTTP pipeline.
/// </summary>
public sealed class RegisterTests : IntegrationTestBase
{
    private const string Endpoint = "/api/auth/register";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static string UniqueEmail() => $"jane.{Guid.NewGuid():N}@example.com";

    // AC-001.1
    [Fact]
    public async Task Register_ValidData_Returns201WithUserDto()
    {
        var payload = new
        {
            email = UniqueEmail(),
            name = "Jane Doe",
            password = "Str0ng!Pass",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(payload.email, body.Email);
        Assert.Equal(payload.name, body.Name);
        Assert.NotEqual(default, body.CreatedAt);
    }

    // AC-001.2
    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        var email = UniqueEmail();
        var payload = new
        {
            email,
            name = "Jane Doe",
            password = "Str0ng!Pass",
        };

        var firstResponse = await Client.PostAsJsonAsync(Endpoint, payload);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await Client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var body = await secondResponse.Content.ReadFromJsonAsync<AssertErrorResponse.ErrorBody>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal(409, body!.Status);
        Assert.Equal("CONFLICT", body.Error);
        Assert.Equal("An account with this email already exists.", body.Message);
        Assert.Empty(body.Details);
    }

    // AC-001.5
    [Fact]
    public async Task Register_InvalidEmail_Returns400WithFieldErrors()
    {
        var payload = new
        {
            email = "not-an-email",
            name = "Jane Doe",
            password = "Str0ng!Pass",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Contains(body.Details, d => d.Field == "email");
    }

    // AC-001.6
    [Fact]
    public async Task Register_UppercaseEmail_Returns400()
    {
        // Per Decision #4: reject, do NOT normalize/lowercase server-side.
        var payload = new
        {
            email = "Jane@Example.com",
            name = "Jane Doe",
            password = "Str0ng!Pass",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Contains(body.Details, d => d.Field == "email");
    }

    // AC-001.3
    [Fact]
    public async Task Register_WeakPassword_Returns400WithAllViolations()
    {
        // "abc" fails min-length, uppercase, digit, and special-char rules
        // simultaneously. CascadeMode.Continue must report all of them, not
        // short-circuit on the first broken rule.
        var payload = new
        {
            email = UniqueEmail(),
            name = "Jane Doe",
            password = "abc",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.True(
            body.Details.Count >= 4,
            $"Expected at least 4 password violations, got {body.Details.Count}: " +
            string.Join(", ", body.Details.Select(d => $"{d.Field}:{d.Issue}")));
        Assert.All(body.Details, d => Assert.Equal("password", d.Field));
    }

    // AC-001.4
    [Fact]
    public async Task Register_MissingAllFields_Returns400WithMultipleErrors()
    {
        var payload = new
        {
            email = "",
            name = "",
            password = "",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Equal(3, body.Details.Count);
        Assert.Contains(body.Details, d => d.Field == "email");
        Assert.Contains(body.Details, d => d.Field == "name");
        Assert.Contains(body.Details, d => d.Field == "password");
    }

    // AC-001.7
    [Fact]
    public async Task Register_NameTooLong_Returns400()
    {
        var payload = new
        {
            email = UniqueEmail(),
            name = new string('a', 101),
            password = "Str0ng!Pass",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Contains(body.Details, d => d.Field == "name");
    }

    // AC-001.7
    [Fact]
    public async Task Register_NameWhitespaceOnly_Returns400()
    {
        var payload = new
        {
            email = UniqueEmail(),
            name = "   ",
            password = "Str0ng!Pass",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Contains(body.Details, d => d.Field == "name");
    }

    // AC-001.1 (security)
    [Fact]
    public async Task Register_PasswordHashNotExposedInResponse()
    {
        var payload = new
        {
            email = UniqueEmail(),
            name = "Jane Doe",
            password = "Str0ng!Pass",
        };

        var response = await Client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var actualProperties = root.EnumerateObject().Select(p => p.Name).ToArray();

        var expectedProperties = new[] { "id", "email", "name", "createdAt" };

        Assert.Equal(expectedProperties.Length, actualProperties.Length);
        foreach (var expected in expectedProperties)
        {
            Assert.Contains(expected, actualProperties);
        }

        Assert.DoesNotContain(
            actualProperties,
            name => name.Contains("password", StringComparison.OrdinalIgnoreCase)
                || name.Contains("hash", StringComparison.OrdinalIgnoreCase));
    }
}
