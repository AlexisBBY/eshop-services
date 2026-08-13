using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Order.API.Models;

namespace Order.API.Data
{
    public class MongoOrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<PurchaseOrder> _orders;

        public MongoOrderRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _orders = database.GetCollection<PurchaseOrder>(settings.Value.CollectionName);
        }

        public async Task<PurchaseOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _orders.Find(o => o.Id == id).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<PurchaseOrder?> GetByIdempotencyKeyAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            return await _orders
                .Find(o => o.CustomerId == customerId && o.IdempotencyKey == idempotencyKey)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<PurchaseOrder>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            return await _orders
                .Find(o => o.CustomerId == customerId)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task CreateAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
        {
            await _orders.InsertOneAsync(order, cancellationToken: cancellationToken);
        }

        public async Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default)
        {
            await _orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: cancellationToken);
        }
    }
}