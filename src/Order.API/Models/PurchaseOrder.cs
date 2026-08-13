using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Order.API.Models
{
    public class PurchaseOrder
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string CustomerId { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public List<OrderItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        // Usado para garantizar idempotencia en POST /api/orders (header Idempotency-Key).
        public string? IdempotencyKey { get; set; }
    }
}