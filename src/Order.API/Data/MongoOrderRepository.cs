using BuildingBlocks.Exceptions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Order.API.Models;

namespace Order.API.Data
{
    public class MongoOrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<PurchaseOrder> _orders;
        private readonly ILogger<MongoOrderRepository> _logger;

        public MongoOrderRepository(IOptions<MongoDbSettings> settings, ILogger<MongoOrderRepository> logger)
        {
            _logger = logger;
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _orders = database.GetCollection<PurchaseOrder>(settings.Value.CollectionName);
        }

        // Convierte cualquier fallo del driver de MongoDB (timeout, conexion, auth, etc.)
        // en un error generico: el detalle interno del driver no debe llegar al cliente.
        private async Task<T> RunAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException)
            {
                _logger.LogError(ex, "Fallo de conexion con MongoDB");
                throw new InternalServerException("No se pudo completar la operacion: la base de datos no esta disponible en este momento.");
            }
        }

        public Task<PurchaseOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            RunAsync(() => _orders.Find(o => o.Id == id).FirstOrDefaultAsync(cancellationToken));

        public Task<PurchaseOrder?> GetByIdempotencyKeyAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            RunAsync(() => _orders
                .Find(o => o.CustomerId == customerId && o.IdempotencyKey == idempotencyKey)
                .FirstOrDefaultAsync(cancellationToken));

        public Task<List<PurchaseOrder>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default) =>
            RunAsync(() => _orders
                .Find(o => o.CustomerId == customerId)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken));

        public Task CreateAsync(PurchaseOrder order, CancellationToken cancellationToken = default) =>
            RunAsync<object?>(async () => { await _orders.InsertOneAsync(order, cancellationToken: cancellationToken); return null; });

        public Task UpdateAsync(PurchaseOrder order, CancellationToken cancellationToken = default) =>
            RunAsync<object?>(async () => { await _orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: cancellationToken); return null; });
    }
}