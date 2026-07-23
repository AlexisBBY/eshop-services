namespace Catalog.API.Models.Products.CreateProduct
{
    public record CreateProductCommand(string Name, string Description,List<string> Category,string ImageFile,decimal Price)
        : ICommand<CreateProductResult>;
    public record CreateProductResult(Guid Id);
    internal class CreateProductCommandHandler(IDocumentSession documentSession): ICommandHandler<CreateProductCommand, CreateProductResult>
    {
        private static readonly List<Product> DemoProducts =
        [
            new() { Id = Guid.NewGuid(), Name = "Laptop", Description = "Laptop de ejemplo", Category = ["Tecnología"], ImageFile = "laptop.png", Price = 1200m },
            new() { Id = Guid.NewGuid(), Name = "Mouse", Description = "Mouse inalámbrico", Category = ["Accesorios"], ImageFile = "mouse.png", Price = 45m },
            new() { Id = Guid.NewGuid(), Name = "Teclado", Description = "Teclado mecánico", Category = ["Accesorios"], ImageFile = "keyboard.png", Price = 89m }
        ];

        public async Task<CreateProductResult> Handle(CreateProductCommand request,CancellationToken cancellationToken)
        {
            try
            {
                Product product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    ImageFile = request.ImageFile,
                    Price = request.Price
                };
                documentSession.Store(product);
                await documentSession.SaveChangesAsync(cancellationToken);
                return new CreateProductResult(product.Id);
            }
            catch
            {
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    ImageFile = request.ImageFile,
                    Price = request.Price
                };
                DemoProducts.Add(product);
                return new CreateProductResult(product.Id);
            }
        }
    }

}
