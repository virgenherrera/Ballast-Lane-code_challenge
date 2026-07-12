using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.ValueObjects;
using TaskFlow.Infrastructure.Identity;

namespace TaskFlow.Infrastructure.Tests.Identity;

public class JwtTokenServiceTests
{
    // At least 32 chars — required for HS256's 256-bit symmetric key.
    private const string TestSecret = "unit-test-signing-secret-at-least-32-chars-long";
    private const string TestIssuer = "taskflow-tests-issuer";
    private const string TestAudience = "taskflow-tests-audience";
    private const int TestExpirySeconds = 900;

    private static JwtOptions CreateOptions()
    {
        return new JwtOptions
        {
            Secret = TestSecret,
            Issuer = TestIssuer,
            Audience = TestAudience,
            ExpirySeconds = TestExpirySeconds,
        };
    }

    private static JwtTokenService CreateSut(JwtOptions? options = null)
    {
        return new JwtTokenService(Options.Create(options ?? CreateOptions()));
    }

    private static User CreateUser()
    {
        var email = Email.Create("jane.doe@example.com");
        var passwordHash = PasswordHash.Create(
            "$2a$12$CwTycUXWue0Thq9StjUM0uJ8Nlq/HJ/PXtL5DsAmxOM.MRp7z3Y0i");

        return User.Create(email, "Jane Doe", passwordHash);
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsWellFormedJwt()
    {
        var sut = CreateSut();
        var user = CreateUser();

        var token = sut.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
        var exception = Record.Exception(() => new JwtSecurityTokenHandler().ReadJwtToken(token));
        Assert.Null(exception);
    }

    [Fact]
    public void GenerateToken_ValidUser_ContainsSubClaimMatchingUserId()
    {
        var sut = CreateSut();
        var user = CreateUser();

        var token = sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var subClaim = parsed.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.Equal(user.Id.ToString(), subClaim.Value);
    }

    [Fact]
    public void GenerateToken_ValidUser_ContainsEmailClaimMatchingUserEmail()
    {
        var sut = CreateSut();
        var user = CreateUser();

        var token = sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var emailClaim = parsed.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.Equal(user.Email.Value, emailClaim.Value);
    }

    [Fact]
    public void GenerateToken_ValidUser_ContainsNameClaimMatchingUserName()
    {
        var sut = CreateSut();
        var user = CreateUser();

        var token = sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var nameClaim = parsed.Claims.Single(c => c.Type == "name");
        Assert.Equal(user.Name, nameClaim.Value);
    }

    [Fact]
    public void GenerateToken_ValidUser_HasExpiryApproximately900SecondsFromNow()
    {
        var sut = CreateSut();
        var user = CreateUser();
        var expectedExpiry = DateTime.UtcNow.AddSeconds(TestExpirySeconds);

        var token = sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var difference = (parsed.ValidTo - expectedExpiry).Duration();
        Assert.True(difference < TimeSpan.FromSeconds(5), $"Expiry drift too large: {difference}");
    }

    [Fact]
    public void GenerateToken_ValidUser_SignedWithHmacSha256()
    {
        var sut = CreateSut();
        var user = CreateUser();

        var token = sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(SecurityAlgorithms.HmacSha256, parsed.Header.Alg);
    }

    [Fact]
    public void GenerateToken_ValidUser_IssuerAndAudienceMatchConfiguredOptions()
    {
        var options = CreateOptions();
        var sut = CreateSut(options);
        var user = CreateUser();

        var token = sut.GenerateToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(options.Issuer, parsed.Issuer);
        Assert.Equal(options.Audience, Assert.Single(parsed.Audiences));
    }
}
