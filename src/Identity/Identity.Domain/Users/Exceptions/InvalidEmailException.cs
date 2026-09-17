namespace Identity.Domain.Users.Exceptions;

public sealed class InvalidEmailException : Exception
{
    public InvalidEmailException(string email) : base($"'{email}' is not a valid email address.") { }
}
