using Order.API.Models;

namespace Order.API.Data
{
    public interface IOrderRepository
    {
        Task<PurchaseOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<PurchaseOrder?> GetByIdempotencyKeyAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken = default);
        Task<List<PurchaseOrder>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
        Task CreateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
        Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default);
    }
}