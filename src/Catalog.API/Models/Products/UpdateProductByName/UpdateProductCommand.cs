using Catalog.API.Exceptions;
using Catalog.API.Models.Products;

namespace Catalog.API.Products.UpdateProducts
{
    public record UpdateProductCommand(Guid Id, string Name, List<string> Category,
        string Description, string ImageFile, decimal Price) :
        ICommand<UpdateProductResult>;

    public record UpdateProductResult(bool IsSuccess);

    /// <summary>
    /// Esta clase se encargara de establecer el orden de actualizacion del producto.
    /// </summary>
    internal class UpdateProductCommandHandler(IDocumentSession session,
        ILogger<UpdateProductCommandHandler> logger)
        : ICommandHandler<UpdateProductCommand, UpdateProductResult>
    {
        private static readonly List<Product> DemoProducts =
        [
            new() { Id = Guid.NewGuid(), Name = "Laptop", Description = "Laptop de ejemplo", Category = ["Tecnología"], ImageFile = "laptop.png", Price = 1200m },
            new() { Id = Guid.NewGuid(), Name = "Mouse", Description = "Mouse inalámbrico", Category = ["Accesorios"], ImageFile = "mouse.png", Price = 45m },
            new() { Id = Guid.NewGuid(), Name = "Teclado", Description = "Teclado mecánico", Category = ["Accesorios"], ImageFile = "keyboard.png", Price = 89m }
        ];

        public async Task<UpdateProductResult> Handle(UpdateProductCommand command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("UpdateProductHandler.Handle llamado con {@Command}", command);
            try
            {
                var product = await session.LoadAsync<Product>(command.Id, cancellationToken);
                if (product is null)
                {
                    throw new ProductNotFoundException();
                }

                product.Name = command.Name;
                product.Category = command.Category;
                product.Description = command.Description;
                product.ImageFile = command.ImageFile;
                product.Price = command.Price;

                session.Update(product);
                await session.SaveChangesAsync(cancellationToken);

                return new UpdateProductResult(true);
            }
            catch
            {
                var product = DemoProducts.FirstOrDefault(p => p.Id == command.Id);
                if (product is null)
                    throw new ProductNotFoundException();

                product.Name = command.Name;
                product.Category = command.Category;
                product.Description = command.Description;
                product.ImageFile = command.ImageFile;
                product.Price = command.Price;

                return new UpdateProductResult(true);
            }
        }
    }
}