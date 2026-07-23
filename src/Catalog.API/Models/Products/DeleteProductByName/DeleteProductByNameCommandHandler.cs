using Catalog.API.Exceptions;
using FluentValidation;

namespace Catalog.API.Models.Products.DeleteProductByName
{
    public record DeleteProductByNameCommand(string Name) : ICommand<DeleteProductByNameResult>;
    public record DeleteProductByNameResult(bool Success);

    // Mismo patrón que DeleteBasketCommandvalidator (Basket.API)
    public class DeleteProductByNameCommandValidator : AbstractValidator<DeleteProductByNameCommand>
    {
        public DeleteProductByNameCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del producto es requerido.");
        }
    }

    internal class DeleteProductByNameCommandHandler(IDocumentSession documentSession, ILogger<DeleteProductByNameCommandHandler> logger)
        : ICommandHandler<DeleteProductByNameCommand, DeleteProductByNameResult>
    {
        private static readonly List<Product> DemoProducts =
        [
            new() { Id = Guid.NewGuid(), Name = "Laptop", Description = "Laptop de ejemplo", Category = ["Tecnología"], ImageFile = "laptop.png", Price = 1200m },
            new() { Id = Guid.NewGuid(), Name = "Mouse", Description = "Mouse inalámbrico", Category = ["Accesorios"], ImageFile = "mouse.png", Price = 45m },
            new() { Id = Guid.NewGuid(), Name = "Teclado", Description = "Teclado mecánico", Category = ["Accesorios"], ImageFile = "keyboard.png", Price = 89m }
        ];

        public async Task<DeleteProductByNameResult> Handle(DeleteProductByNameCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("DeleteProductByNameCommandHandler.Handle llamado con {@Request}", request);

            try
            {
                var product = await documentSession.Query<Product>()
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower(), cancellationToken);

                if (product is null)
                    throw new ProductNotFoundException(request.Name);

                documentSession.Delete(product);
                await documentSession.SaveChangesAsync(cancellationToken);

                return new DeleteProductByNameResult(true);
            }
            catch
            {
                var product = DemoProducts.FirstOrDefault(p => p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
                if (product is null)
                    throw new ProductNotFoundException(request.Name);

                DemoProducts.Remove(product);
                return new DeleteProductByNameResult(true);
            }
        }
    }
}
