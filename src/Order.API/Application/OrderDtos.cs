using Order.API.Models;

namespace Order.API.Application
{
    public record CreateOrderRequest(string CustomerId, string BasketId);

    public record OrderItemResponse(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

    public record OrderResponse(
        string Id,
        string CustomerId,
        DateTime CreatedAt,
        string Status,
        List<OrderItemResponse> Items,
        decimal Subtotal,
        decimal Tax,
        decimal Total)
    {
        public static OrderResponse FromOrder(PurchaseOrder order) => new(
            order.Id,
            order.CustomerId,
            order.CreatedAt,
            order.Status.ToString(),
            order.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal)).ToList(),
            order.Subtotal,
            order.Tax,
            order.Total);
    }

    public record UpdateOrderStatusRequest(string Status);
}