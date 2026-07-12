namespace TaskFlow.Domain.Exceptions;

public sealed class DuplicateEmailException : DomainException
{
    public DuplicateEmailException()
        : base("An account with this email already exists.")
    {
    }
}
