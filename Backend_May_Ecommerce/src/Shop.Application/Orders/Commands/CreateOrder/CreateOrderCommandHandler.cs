using MediatR;
using Shop.Domain.Entities;
using Shop.Domain.Exceptions;
using Shop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Shop.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(IOrderRepository orderRepository,IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.UserId);

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product == null)
                throw new KeyNotFoundException($"Product with ID {item.ProductId} was not found");
            product.DecreaseStock(item.Quantity);

            order.AddItem(product.Id, product.Name, product.Price, item.Quantity);
        }


        await _orderRepository.AddAsync(order, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangeAsync(cancellationToken);
        }

        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOrderException("Order creation failed due to concurrent inventory modification. Please retry your order.");
        }


        return order.Id;
    }
}
