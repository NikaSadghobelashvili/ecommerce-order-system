using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default);
}
