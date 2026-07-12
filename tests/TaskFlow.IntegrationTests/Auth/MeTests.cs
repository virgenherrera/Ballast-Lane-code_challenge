using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.API.Contracts;
using TaskFlow.API.Contracts.Auth;
using TaskFlow.IntegrationTests.Common;

namespace TaskFlow.IntegrationTests.Auth;

/// <summary>
/// Full integration suite for GET /api/auth/me, run against a real
/// PostgreSQL Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no
/// InMemory/SQLite providers). Proves AC-003.1 (Decision #6): the endpoint
/// is [Authorize]-protected while Register/Login remain public, and returns
/// the authenticated caller's own profile.
/// </summary>
public sealed class MeTests : IntegrationTestBase
{
    private const string RegisterEndpoint = "/api/auth/register";
    private const string LoginEndpoint = "/api/auth/login";
    private const string MeEndpoint = "/api/auth/me";

    // Must match TaskFlowWebApplicationFactory's env vars exactly so a
    // manually-crafted token validates (or is deliberately made not to)
    // against the same TokenValidationParameters as Program.cs configures.
    private const string TestJwtSecret = "test-harness-secret-not-for-production-use";
    private const string TestJwtIssuer = "taskflow-test-harness";
    private const string TestJwtAudience = "taskflow-test-harness";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private async Task<(string Email, string Name, string AccessToken)> RegisterAndLoginAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string name = "Test User";
        const string password = "ValidPass1!";

        var registerResponse = await Client.PostAsJsonAsync(
            RegisterEndpoint,
            new { email, name, password });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await Client.PostAsJsonAsync(LoginEndpoint, new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(CaseInsensitive);
        Assert.NotNull(body);

        return (email, name, body!.AccessToken);
    }

    // Builds a self-signed JWT with the same signing key/issuer/audience the
    // API validates against, but with an explicit (possibly already-elapsed)
    // expiry — used to exercise the expired-token path without waiting on a
    // real clock.
    private static string BuildToken(Guid userId, string email, string name, TimeSpan expiresIn)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("name", name),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AssertStandard401(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AC-003.1
    [Fact]
    public async Task GetMe_WithValidToken_Returns200WithUserProfile()
    {
        var (email, name, accessToken) = await RegisterAndLoginAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await Client.GetAsync(MeEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(email, body.Email);
        Assert.Equal(name, body.Name);
        Assert.NotEqual(default, body.CreatedAt);
    }

    // AC-003.1 (security)
    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync(MeEndpoint);

        AssertStandard401(response);
    }

    // AC-003.1 (security)
    [Fact]
    public async Task GetMe_WithExpiredToken_Returns401()
    {
        var expiredToken = BuildToken(
            Guid.NewGuid(),
            "expired@example.com",
            "Expired User",
            TimeSpan.FromSeconds(-1));

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await Client.GetAsync(MeEndpoint);

        AssertStandard401(response);
    }

    // AC-003.1 (security)
    [Fact]
    public async Task GetMe_WithTamperedToken_Returns401()
    {
        var (_, _, accessToken) = await RegisterAndLoginAsync();

        // Flip a single character in the signature segment so the token is
        // structurally valid (three dot-separated segments) but fails
        // signature verification.
        var segments = accessToken.Split('.');
        Assert.Equal(3, segments.Length);
        var signature = segments[2];
        var tamperedChar = signature[0] == 'A' ? 'B' : 'A';
        segments[2] = tamperedChar + signature[1..];
        var tamperedToken = string.Join('.', segments);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await Client.GetAsync(MeEndpoint);

        AssertStandard401(response);
    }
}
