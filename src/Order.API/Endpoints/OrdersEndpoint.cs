using Carter;
using Order.API.Application;

namespace Order.API.Endpoints
{
    public class OrdersEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/orders").WithTags("Orders");

            group.MapPost("/", async (CreateOrderRequest request, HttpRequest httpRequest, IOrderService service, CancellationToken ct) =>
            {
                string? idempotencyKey = httpRequest.Headers.TryGetValue("Idempotency-Key", out var key) ? key.ToString() : null;
                var order = await service.CreateOrderAsync(request, idempotencyKey, ct);
                return Results.Created($"/api/orders/{order.Id}", order);
            })
            .WithName("CreateOrder")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Crea una orden de compra a partir del carrito del cliente")
            .WithDescription("Valida el carrito y los productos, congela los precios, y persiste la orden en MongoDB. " +
                "Soporta idempotencia mediante el header Idempotency-Key.");

            group.MapGet("/", async (IOrderService service, CancellationToken ct) =>
            {
                var orders = await service.GetAllAsync(ct);
                return Results.Ok(orders);
            })
            .WithName("GetAllOrders")
            .Produces<List<OrderResponse>>(StatusCodes.Status200OK)
            .WithSummary("Lista todas las ordenes");

            group.MapGet("/{id}", async (string id, IOrderService service, CancellationToken ct) =>
            {
                var order = await service.GetByIdAsync(id, ct);
                return Results.Ok(order);
            })
            .WithName("GetOrderById")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Consulta una orden por su identificador");

            group.MapGet("/customer/{customerId}", async (string customerId, IOrderService service, CancellationToken ct) =>
            {
                var orders = await service.GetByCustomerIdAsync(customerId, ct);
                return Results.Ok(orders);
            })
            .WithName("GetOrdersByCustomer")
            .Produces<List<OrderResponse>>(StatusCodes.Status200OK)
            .WithSummary("Lista las ordenes de un cliente");

            group.MapPatch("/{id}/status", async (string id, UpdateOrderStatusRequest request, IOrderService service, CancellationToken ct) =>
            {
                var order = await service.UpdateStatusAsync(id, request.Status, ct);
                return Results.Ok(order);
            })
            .WithName("UpdateOrderStatus")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Cambia el estado de una orden validando la transicion")
            .WithDescription("Transiciones validas: Pending -> Confirmed, Pending -> Cancelled.");
        }
    }
}