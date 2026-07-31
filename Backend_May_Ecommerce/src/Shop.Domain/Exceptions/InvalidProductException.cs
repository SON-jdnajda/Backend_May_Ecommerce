namespace Shop.Domain.Exceptions;

public class InvalidProductException : DomainException
{
    public InvalidProductException(string message) : base(message) { }
}
