using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Domain.Tests.Entities;

public class UserTests
{
    private static Email ValidEmail => Email.Create("jane@example.com");

    private static PasswordHash ValidPasswordHash => PasswordHash.Create("hashed-value");

    [Fact]
    public void User_Create_ValidData_AssignsUuidV7AndCreatedAt()
    {
        var email = ValidEmail;
        var passwordHash = ValidPasswordHash;

        var user = User.Create(email, "Jane", passwordHash);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.NotEqual(default, user.CreatedAt);
        Assert.Equal("Jane", user.Name);
        Assert.Equal(email, user.Email);
        Assert.Same(passwordHash, user.PasswordHash);
    }

    [Fact]
    public void User_Create_TrimsNameBeforeStorage()
    {
        var user = User.Create(ValidEmail, "  Alice  ", ValidPasswordHash);

        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void User_Create_WhitespaceOnlyName_ThrowsDomainException()
    {
        Assert.Throws<InvalidUserNameException>(
            () => User.Create(ValidEmail, "   ", ValidPasswordHash));
    }

    [Fact]
    public void User_Create_TwoInstances_HaveDistinctTimeOrderedIds()
    {
        var first = User.Create(ValidEmail, "Jane", ValidPasswordHash);
        var second = User.Create(ValidEmail, "Jane", ValidPasswordHash);

        Assert.NotEqual(first.Id, second.Id);

        // UUID v7's leading 48 bits are a big-endian millisecond timestamp (RFC 9562),
        // giving the overall value a time-ordered *prefix*. Two calls within the same
        // millisecond share that prefix and are ordered only by random tail bits, which
        // RFC 9562 does NOT guarantee to be monotonic without a counter extension — so
        // strict byte-for-byte ordering cannot be asserted for rapid successive calls.
        // Instead assert the invariant that IS guaranteed: the timestamp prefix never
        // goes backwards.
        var firstTimestampMs = first.Id.ToByteArray(bigEndian: true)[..6];
        var secondTimestampMs = second.Id.ToByteArray(bigEndian: true)[..6];

        Assert.True(
            new ReadOnlySpan<byte>(secondTimestampMs).SequenceCompareTo(firstTimestampMs) >= 0);
    }
}
