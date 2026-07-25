namespace Shop.Domain.Exceptions;
public class InsufficientStockException : DomainException
{
     public Guid ProductId {get;}
     public int Available {get;}
     public int Requested {get;}

     public InsufficientStockException(Guid productId, int available, int requested)
          :base($"Sản phẩm {productId} không đủ hàng. Còn lại {available}, yêu cầu {requested}")
     {
          ProductId = productId;
          Available = available;
          Requested = requested;
     }
}