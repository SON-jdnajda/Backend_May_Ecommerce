using FluentAssertions;
using NSubstitute;
using Shop.Application.Orders.Commands.CreateOrder;
using Shop.Domain.Entities;
using Shop.Domain.Exceptions;
using Shop.Domain.Repositories;

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

    private void GivenProducts(params Product[] products) =>
        _productRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(products.ToList());

    [Fact]
    public async Task Handle_ShouldCreateOrderAndDecreaseProductStock_WhenCommandIsValid()
    {
        // Arrange
        var product = new Product("Laptop", "Gaming Laptop", 1000m, 10, Guid.NewGuid());
        GivenProducts(product);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(product.Id, 2)]);

        // Act
        var orderId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        orderId.Should().NotBeEmpty();
        product.StockQuantity.Should().Be(8);

        await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldBumpConcurrencyToken_WhenStockIsDecreased()
    {
        // The optimistic-concurrency token is worthless unless every stock
        // mutation advances it - this is the test that would have caught the
        // "IsRequired() instead of IsConcurrencyToken()" bug staying invisible.
        var product = new Product("Laptop", null, 1000m, 10, Guid.NewGuid());
        var versionBefore = product.Version;
        GivenProducts(product);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(product.Id, 1)]);

        await _handler.Handle(command, CancellationToken.None);

        product.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public async Task Handle_ShouldPriceFromDatabase_NotFromRequest()
    {
        // The command carries no price at all, so the order total can only come
        // from Product.Price. 2 x 1000 = 2000.
        var product = new Product("Laptop", null, 1000m, 10, Guid.NewGuid());
        GivenProducts(product);

        Order? captured = null;
        await _orderRepository.AddAsync(
            Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(product.Id, 2)]);

        await _handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TotalAmount.Should().Be(2000m);
        captured.OrderItems.Single().UnitPrice.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOrderException_WhenProductDoesNotExist()
    {
        // Arrange
        var missingProductId = Guid.NewGuid();
        GivenProducts();

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(missingProductId, 1)]);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOrderException>()
            .WithMessage($"Product '{missingProductId}' does not exist");
    }

    [Fact]
    public async Task Handle_ShouldThrowInsufficientStock_WhenOrderExceedsAvailableQuantity()
    {
        var product = new Product("Laptop", null, 1000m, 3, Guid.NewGuid());
        GivenProducts(product);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(product.Id, 5)]);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientStockException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPropagateConcurrencyConflict_WhenSaveLosesTheRace()
    {
        // The handler deliberately does NOT catch this: CustomExceptionHandler
        // maps ConcurrencyConflictException to 409 Conflict. Note the test needs
        // no EF Core reference - that is the point of translating at the boundary.
        var product = new Product("Smartphone", "Desc", 500m, 5, Guid.NewGuid());
        GivenProducts(product);

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => throw new ConcurrencyConflictException("The data changed while your request was being processed. Please retry."));

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemDto(product.Id, 1)]);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
