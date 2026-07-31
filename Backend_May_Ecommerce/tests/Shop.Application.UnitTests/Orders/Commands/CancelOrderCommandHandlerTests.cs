using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shop.Application.Orders.Commands.CreateOrder;
using Shop.Domain.Entities;
using Shop.Domain.Exceptions;
using Shop.Domain.Repositories;
using Xunit;

namespace Shop.Application.UnitTests.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _handler = new CreateOrderCommandHandler(
            _orderRepository,
            _productRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrderAndDecreaseProductStock_WhenCommandIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = new Product("Laptop", "Gaming Laptop", 1000m, 10, Guid.NewGuid());

        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        var command = new CreateOrderCommand(
            userId,
            new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto(productId, "Laptop", 1000m, 2)
            });

        // Act
        var orderId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        orderId.Should().NotBeEmpty();
        product.StockQuantity.Should().Be(8); // Stock decreased from 10 to 8

        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();

        _productRepository.GetByIdAsync(nonExistentProductId, Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto(nonExistentProductId, "Unknown Product", 100m, 1)
            });

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Product with ID {nonExistentProductId} was not found");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOrderException_WhenConcurrencyConflictOccurs()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product("Smartphone", "Desc", 500m, 5, Guid.NewGuid());

        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        // Simulate EF Core throwing DbUpdateConcurrencyException when saving changes
        _unitOfWork.When(x => x.SaveChangeAsync(Arg.Any<CancellationToken>()))
            .Do(x => throw new DbUpdateConcurrencyException("Concurrency conflict", new List<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>()));

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            new List<CreateOrderItemDto> { new CreateOrderItemDto(productId, "Smartphone", 500m, 1) });

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOrderException>()
            .WithMessage("Order creation failed due to concurrent inventory modification. Please retry your order.");
    }

}
