namespace Shop.Domain.Exceptions;

public class InvalidCategoryException : DomainException
{
     public InvalidCategoryException(string message) : base(message) {}
}
