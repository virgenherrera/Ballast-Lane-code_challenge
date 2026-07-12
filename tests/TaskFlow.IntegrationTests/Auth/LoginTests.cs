using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.API.Contracts;
using TaskFlow.IntegrationTests.Common;

namespace TaskFlow.IntegrationTests.Auth;

/// <summary>
/// Full integration suite for POST /api/auth/login, run against a real
/// PostgreSQL Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no
/// InMemory/SQLite providers). Proves every AC-002.x acceptance criterion
/// end-to-end: successful login with token + user summary, generic 401 for
/// both failure paths (never split — TASKFLOW-ANTI-DRIFT), required-field
/// validation, weak-password acceptance at login (no strength re-check),
/// rate limiting at 429 after 5 attempts/min/IP, and JWT claim/expiry
/// correctness.
/// </summary>
public sealed class LoginTests : IntegrationTestBase
{
    private const string RegisterEndpoint = "/api/auth/register";
    private const string LoginEndpoint = "/api/auth/login";

    // Named constant, single source of truth — never re-derive 900 independently
    // (TASKFLOW-ANTI-DRIFT: expiresIn is a frozen 15-minute decision).
    private const int ExpectedExpiresInSeconds = 900;

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Registers a user via the real endpoint, returns (email, password) used,
    // so each test can log in against a known-good account without touching
    // the DB directly (black-box, per RegisterTests.cs pattern).
    private async Task<(string Email, string Password)> RegisterUserAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "ValidPass1!";
        var payload = new { email, name = "Test User", password };
        var response = await Client.PostAsJsonAsync(RegisterEndpoint, payload);
        response.EnsureSuccessStatusCode();
        return (email, password);
    }

    // Base64url-decodes the JWT payload segment (middle of the three dot-separated
    // segments) and parses it as JSON. No signature verification is performed here —
    // claim/expiry inspection does not require it (that is Batch 5's concern).
    private static JsonDocument DecodeJwtPayload(string jwt)
    {
        var segments = jwt.Split('.');
        Assert.Equal(3, segments.Length);

        var payloadSegment = segments[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payloadSegment.Length % 4)
        {
            case 2:
                payloadSegment += "==";
                break;
            case 3:
                payloadSegment += "=";
                break;
        }

        var payloadBytes = Convert.FromBase64String(payloadSegment);
        return JsonDocument.Parse(payloadBytes);
    }

    // AC-002.1
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokenAndUser()
    {
        var (email, password) = await RegisterUserAsync();
        var payload = new { email, password };

        var response = await Client.PostAsJsonAsync(LoginEndpoint, payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.Equal("Bearer", body.TokenType);
        Assert.Equal(ExpectedExpiresInSeconds, body.ExpiresIn);
        Assert.NotNull(body.User);
        Assert.NotEqual(Guid.Empty, body.User.Id);
        Assert.Equal(email, body.User.Email);
        Assert.Equal("Test User", body.User.Name);
    }

    // AC-002.2
    [Fact]
    public async Task Login_NonExistentEmail_Returns401Generic()
    {
        var payload = new
        {
            email = $"never-registered-{Guid.NewGuid():N}@example.com",
            password = "WhateverPass1!",
        };

        var response = await Client.PostAsJsonAsync(LoginEndpoint, payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AssertErrorResponse.ErrorBody>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal(401, body!.Status);
        Assert.Equal("UNAUTHORIZED", body.Error);
        Assert.Equal("Invalid email or password.", body.Message);
        Assert.Empty(body.Details);
    }

    // AC-002.2
    [Fact]
    public async Task Login_WrongPassword_Returns401Generic()
    {
        var (email, _) = await RegisterUserAsync();
        var payload = new { email, password = "WrongPassword1!" };

        var response = await Client.PostAsJsonAsync(LoginEndpoint, payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AssertErrorResponse.ErrorBody>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal(401, body!.Status);
        Assert.Equal("UNAUTHORIZED", body.Error);
        Assert.Equal("Invalid email or password.", body.Message);
        Assert.Empty(body.Details);
    }

    // AC-002.2 (security — cross-test assertion). Proves no user-enumeration leak:
    // "user not found" and "wrong password" must be indistinguishable to a client.
    [Fact]
    public async Task Login_BothFailurePaths_ReturnIdenticalErrorMessage()
    {
        var (email, _) = await RegisterUserAsync();

        var nonExistentResponse = await Client.PostAsJsonAsync(
            LoginEndpoint,
            new { email = $"never-registered-{Guid.NewGuid():N}@example.com", password = "WhateverPass1!" });

        var wrongPasswordResponse = await Client.PostAsJsonAsync(
            LoginEndpoint,
            new { email, password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, nonExistentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);

        var nonExistentBody = await nonExistentResponse.Content
            .ReadFromJsonAsync<AssertErrorResponse.ErrorBody>(CaseInsensitive);
        var wrongPasswordBody = await wrongPasswordResponse.Content
            .ReadFromJsonAsync<AssertErrorResponse.ErrorBody>(CaseInsensitive);

        Assert.NotNull(nonExistentBody);
        Assert.NotNull(wrongPasswordBody);

        // Direct string-equality assertion — the core security guarantee.
        Assert.Equal(nonExistentBody!.Message, wrongPasswordBody!.Message);
        Assert.Equal(nonExistentBody.Status, wrongPasswordBody.Status);
        Assert.Equal(nonExistentBody.Error, wrongPasswordBody.Error);
    }

    // AC-002.3
    [Fact]
    public async Task Login_EmptyEmail_Returns400WithFieldError()
    {
        var payload = new { email = "", password = "ValidPass1!" };

        var response = await Client.PostAsJsonAsync(LoginEndpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Contains(body.Details, d => d.Field == "email");
    }

    // AC-002.3
    [Fact]
    public async Task Login_EmptyPassword_Returns400WithFieldError()
    {
        var payload = new { email = "user@example.com", password = "" };

        var response = await Client.PostAsJsonAsync(LoginEndpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Contains(body.Details, d => d.Field == "password");
    }

    // AC-002.3 (login validator does not check strength). A 400 here would be a
    // REGRESSION — it would mean the login validator started re-running
    // registration's password-strength rules, which it must never do.
    [Fact]
    public async Task Login_WeakPassword_PassesValidation_FailsAuth()
    {
        var (email, _) = await RegisterUserAsync();
        var payload = new { email, password = "a" };

        var response = await Client.PostAsJsonAsync(LoginEndpoint, payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Security — rate limiting (Decision #9). Self-contained: makes its own 6
    // sequential calls against this test's own HttpClient/factory instance so it
    // cannot pre-exhaust (or be exhausted by) the limiter used by other tests.
    [Fact]
    public async Task Login_RateLimit_ExceedsFivePerMinute_Returns429()
    {
        var payload = new { email = "rate-limit-probe@example.com", password = "WhateverPass1!" };

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 6; i++)
        {
            lastResponse = await Client.PostAsJsonAsync(LoginEndpoint, payload);
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        Assert.True(
            lastResponse.Headers.TryGetValues("Retry-After", out var retryAfterValues),
            "Expected a Retry-After header on the 429 response.");
        Assert.NotEmpty(retryAfterValues!);
    }

    // AC-002.1 (token structure)
    [Fact]
    public async Task Login_TokenContainsExpectedClaims()
    {
        var (email, password) = await RegisterUserAsync();
        var loginResponse = await Client.PostAsJsonAsync(LoginEndpoint, new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(CaseInsensitive);
        Assert.NotNull(body);

        using var payloadDocument = DecodeJwtPayload(body!.AccessToken);
        var root = payloadDocument.RootElement;

        Assert.Equal(body.User.Id.ToString(), root.GetProperty("sub").GetString());
        Assert.Equal(email, root.GetProperty("email").GetString());
        Assert.Equal("Test User", root.GetProperty("name").GetString());
    }

    // AC-002.1 (expiry)
    [Fact]
    public async Task Login_TokenExpiresIn900Seconds()
    {
        var (email, password) = await RegisterUserAsync();

        var beforeRequest = DateTimeOffset.UtcNow;
        var loginResponse = await Client.PostAsJsonAsync(LoginEndpoint, new { email, password });
        var afterRequest = DateTimeOffset.UtcNow;
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(CaseInsensitive);
        Assert.NotNull(body);

        using var payloadDocument = DecodeJwtPayload(body!.AccessToken);
        var root = payloadDocument.RootElement;

        var exp = root.GetProperty("exp").GetInt64();
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);

        if (root.TryGetProperty("iat", out var iatElement))
        {
            var iat = iatElement.GetInt64();
            Assert.Equal(ExpectedExpiresInSeconds, exp - iat);
        }
        else
        {
            var expectedExpiresAt = beforeRequest.AddSeconds(ExpectedExpiresInSeconds);
            var tolerance = TimeSpan.FromSeconds(5) + (afterRequest - beforeRequest);
            Assert.True(
                Math.Abs((expiresAt - expectedExpiresAt).TotalSeconds) <= tolerance.TotalSeconds,
                $"Expected exp ~{expectedExpiresAt:O}, got {expiresAt:O} (tolerance {tolerance.TotalSeconds}s).");
        }
    }
}
