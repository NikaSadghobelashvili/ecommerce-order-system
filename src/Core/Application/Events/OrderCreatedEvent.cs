namespace Application.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerEmail,
    decimal TotalAmount,
    DateTime CreatedAtUtc);
