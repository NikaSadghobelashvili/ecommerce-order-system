using Application.Events;
using Application.Interfaces;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderCreatedConsumer> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "RABBITMQ CONSUMER: Received OrderCreatedEvent for Order {OrderId} | Customer: {Email} | Amount: {Amount:C}",
            message.OrderId,
            message.CustomerEmail,
            message.TotalAmount);

        // Simulate async background work (e.g., generating PDF invoice & sending email)
        await Task.Delay(1000, context.CancellationToken);

        await _orderRepository.UpdateStatusAsync(message.OrderId, OrderStatus.Completed, context.CancellationToken);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "RABBITMQ CONSUMER: Order {OrderId} processed, email sent to {Email}, status updated to Completed.",
            message.OrderId,
            message.CustomerEmail);
    }
}
