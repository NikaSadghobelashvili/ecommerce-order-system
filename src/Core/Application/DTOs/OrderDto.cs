using Domain.Enums;

namespace Application.DTOs;

public record CreateOrderItemRequest(int ProductId, int Quantity);

public record CreateOrderRequest(
    string CustomerEmail,
    IReadOnlyList<CreateOrderItemRequest> Items);

public record OrderItemDto(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record OrderDto(
    Guid Id,
    string CustomerEmail,
    decimal TotalAmount,
    OrderStatus Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items);
