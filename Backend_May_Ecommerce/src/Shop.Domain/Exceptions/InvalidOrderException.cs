namespace Shop.Domain.Exceptions;

public class InvalidOrderException : DomainException
{
    public InvalidOrderException(string message) : base(message) { }
}   