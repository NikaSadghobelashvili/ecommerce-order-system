using Application.DTOs;
using Application.Events;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IEventBus eventBus,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = request.CustomerEmail,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        decimal totalAmount = 0;

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
            {
                throw new ArgumentException($"Quantity for product {itemReq.ProductId} must be greater than zero.");
            }

            var product = await _productRepository.GetByIdAsync(itemReq.ProductId, cancellationToken)
                ?? throw new KeyNotFoundException($"Product with ID {itemReq.ProductId} was not found.");

            if (product.StockQuantity < itemReq.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}");
            }

            product.StockQuantity -= itemReq.Quantity;

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Product = product,
                Quantity = itemReq.Quantity,
                UnitPrice = product.Price
            };

            order.Items.Add(orderItem);
            totalAmount += product.Price * itemReq.Quantity;

            // Invalidate individual product cache since stock changed
            await _cacheService.RemoveAsync($"products:{product.Id}", cancellationToken);
        }

        order.TotalAmount = totalAmount;

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate all products list cache since stock quantities changed
        await _cacheService.RemoveAsync("products:all", cancellationToken);

        // Publish integration event to RabbitMQ (non-blocking async workflow)
        var orderEvent = new OrderCreatedEvent(
            order.Id,
            order.CustomerEmail,
            order.TotalAmount,
            order.CreatedAtUtc);

        await _eventBus.PublishAsync(orderEvent, cancellationToken);
        _logger.LogInformation("Order {OrderId} created and OrderCreatedEvent published to RabbitMQ.", order.Id);

        return MapToDto(order);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : MapToDto(order);
    }

    private static OrderDto MapToDto(Order order) =>
        new(
            order.Id,
            order.CustomerEmail,
            order.TotalAmount,
            order.Status,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.Product?.Name ?? string.Empty,
                i.Quantity,
                i.UnitPrice)).ToList());
}
