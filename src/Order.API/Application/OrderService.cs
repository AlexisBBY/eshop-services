using BuildingBlocks.Exceptions;
using Order.API.Data;
using Order.API.Models;
using Order.API.Services;

namespace Order.API.Application
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
        Task<OrderResponse> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<List<OrderResponse>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
        Task<OrderResponse> UpdateStatusAsync(string id, string status, CancellationToken cancellationToken = default);
    }

    public class OrderService(
        IOrderRepository repository,
        IBasketApiClient basketClient,
        ICatalogApiClient catalogClient,
        ILogger<OrderService> logger) : IOrderService
    {
        private const decimal TaxRate = 0.16m;

        // Transiciones validas del ciclo de vida de una orden.
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
            [OrderStatus.Confirmed] = [],
            [OrderStatus.Cancelled] = []
        };

        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId) || string.IsNullOrWhiteSpace(request.BasketId))
                throw new BadRequestException("customerId y basketId son requeridos.");

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await repository.GetByIdempotencyKeyAsync(request.CustomerId, idempotencyKey, cancellationToken);
                if (existing is not null)
                {
                    logger.LogInformation("Idempotency-Key {Key} ya existente, devolviendo orden {OrderId}", idempotencyKey, existing.Id);
                    return OrderResponse.FromOrder(existing);
                }
            }

            var basket = await basketClient.GetBasketAsync(request.BasketId, cancellationToken);
            if (basket is null || basket.Items.Count == 0)
                throw new BadRequestException("El carrito esta vacio o no existe.");

            var items = new List<OrderItem>();
            foreach (var basketItem in basket.Items)
            {
                if (basketItem.Quantity <= 0 || basketItem.Price < 0)
                    throw new BadRequestException($"Datos inconsistentes en el producto '{basketItem.ProductName}'.");

                var productExists = await catalogClient.ProductExistsAsync(basketItem.ProductId, cancellationToken);
                if (!productExists)
                    throw new BadRequestException($"Producto inexistente: {basketItem.ProductId}");

                items.Add(new OrderItem
                {
                    ProductId = basketItem.ProductId,
                    ProductName = basketItem.ProductName,
                    Quantity = basketItem.Quantity,
                    UnitPrice = basketItem.Price
                });
            }

            var subtotal = items.Sum(i => i.LineTotal);
            var tax = Math.Round(subtotal * TaxRate, 2);

            var order = new PurchaseOrder
            {
                CustomerId = request.CustomerId,
                Items = items,
                Subtotal = subtotal,
                Tax = tax,
                Total = subtotal + tax,
                IdempotencyKey = idempotencyKey
            };

            await repository.CreateAsync(order, cancellationToken);
            logger.LogInformation("Orden {OrderId} creada para cliente {CustomerId}", order.Id, order.CustomerId);

            return OrderResponse.FromOrder(order);
        }

        public async Task<OrderResponse> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var order = await repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(nameof(PurchaseOrder), id);

            return OrderResponse.FromOrder(order);
        }

        public async Task<List<OrderResponse>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            var orders = await repository.GetByCustomerIdAsync(customerId, cancellationToken);
            return orders.Select(OrderResponse.FromOrder).ToList();
        }

        public async Task<OrderResponse> UpdateStatusAsync(string id, string status, CancellationToken cancellationToken = default)
        {
            var order = await repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException(nameof(PurchaseOrder), id);

            if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var newStatus))
                throw new BadRequestException($"Estado invalido: '{status}'. Valores validos: Pending, Confirmed, Cancelled.");

            if (!AllowedTransitions[order.Status].Contains(newStatus))
                throw new BadRequestException($"Transicion no permitida: {order.Status} -> {newStatus}.");

            order.Status = newStatus;
            await repository.UpdateAsync(order, cancellationToken);

            return OrderResponse.FromOrder(order);
        }
    }
}