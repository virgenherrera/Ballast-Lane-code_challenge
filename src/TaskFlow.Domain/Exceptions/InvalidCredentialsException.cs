namespace TaskFlow.Domain.Exceptions;

public sealed class InvalidCredentialsException : DomainException
{
    // Generic message: MUST NOT contain the words "email", "password", or "user" —
    // verified by a dedicated test. Prevents field-hinting / user enumeration.
    public const string GenericMessage = "Invalid credentials.";

    public InvalidCredentialsException()
        : base(GenericMessage)
    {
    }
}
