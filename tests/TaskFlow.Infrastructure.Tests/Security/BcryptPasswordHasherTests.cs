using Microsoft.Extensions.Options;
using TaskFlow.Domain.ValueObjects;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure.Tests.Security;

public class BcryptPasswordHasherTests
{
    // Work factor 4 keeps the suite fast (~250ms/hash at factor 12 vs. a few ms at factor 4).
    private const int TestWorkFactor = 4;

    private static BcryptPasswordHasher CreateSut(int workFactor = TestWorkFactor)
    {
        return new BcryptPasswordHasher(Options.Create(new BcryptOptions { WorkFactor = workFactor }));
    }

    [Fact]
    public void Hash_ValidPassword_ReturnsHashDifferentFromPlainText()
    {
        var sut = CreateSut();
        const string plainTextPassword = "Correct-Horse-Battery-Staple-1";

        var hash = sut.Hash(plainTextPassword);

        Assert.NotNull(hash);
        Assert.NotEqual(plainTextPassword, hash.Value);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        var sut = CreateSut();
        const string plainTextPassword = "Correct-Horse-Battery-Staple-1";

        var first = sut.Hash(plainTextPassword);
        var second = sut.Hash(plainTextPassword);

        Assert.NotEqual(first.Value, second.Value);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var sut = CreateSut();
        const string plainTextPassword = "Correct-Horse-Battery-Staple-1";
        var hash = sut.Hash(plainTextPassword);

        var result = sut.Verify(plainTextPassword, hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_IncorrectPassword_ReturnsFalse()
    {
        var sut = CreateSut();
        var hash = sut.Hash("Correct-Horse-Battery-Staple-1");

        var result = sut.Verify("Wrong-Password-2", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_EmptyPassword_ReturnsFalse()
    {
        var sut = CreateSut();
        var hash = sut.Hash("Correct-Horse-Battery-Staple-1");

        var result = sut.Verify(string.Empty, hash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_ProducesValidBcryptFormat_StartsWithDollarTwoPrefix()
    {
        var sut = CreateSut();

        var hash = sut.Hash("Correct-Horse-Battery-Staple-1");

        Assert.StartsWith("$2", hash.Value);
    }

    [Fact]
    public void BcryptPasswordHasher_ConstructedWithWorkFactorFour_HashesFasterThanTwelve()
    {
        var fastHasher = CreateSut(workFactor: 4);
        var slowHasher = CreateSut(workFactor: 12);
        const string plainTextPassword = "Correct-Horse-Battery-Staple-1";

        // Warm up the JIT so first-call overhead does not skew either measurement.
        fastHasher.Hash(plainTextPassword);
        slowHasher.Hash(plainTextPassword);

        var fastStart = System.Diagnostics.Stopwatch.StartNew();
        fastHasher.Hash(plainTextPassword);
        fastStart.Stop();

        var slowStart = System.Diagnostics.Stopwatch.StartNew();
        slowHasher.Hash(plainTextPassword);
        slowStart.Stop();

        Assert.True(fastStart.Elapsed < slowStart.Elapsed);
    }
}
