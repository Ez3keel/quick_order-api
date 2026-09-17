namespace Identity.Application.Auth.Exceptions;

public sealed class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException(string email) : base($"Email '{email}' is already registered.") { }
}
