using Catalog.API.Common.Pagination;

namespace Catalog.API.Models.Products.GetProducts
{
    public record GetProductsQuery(string? Name, string? Category = null, decimal? MinPrice = null,
        decimal? MaxPrice = null, int PageIndex = 1, int PageSize = 10) : IQuery<GetProductsResult>;
    public record GetProductsResult(PaginatedResult<Product> Products);

    internal class GetProductsQueryHandler(IDocumentSession session, ILogger<GetProductsQueryHandler> logger)
        : IqueryHandler<GetProductsQuery, GetProductsResult>
    {
        private static readonly List<Product> DemoProducts =
        [
            new() { Id = Guid.NewGuid(), Name = "Laptop", Description = "Laptop de ejemplo", Category = ["Tecnología"], ImageFile = "laptop.png", Price = 1200m },
            new() { Id = Guid.NewGuid(), Name = "Mouse", Description = "Mouse inalámbrico", Category = ["Accesorios"], ImageFile = "mouse.png", Price = 45m },
            new() { Id = Guid.NewGuid(), Name = "Teclado", Description = "Teclado mecánico", Category = ["Accesorios"], ImageFile = "keyboard.png", Price = 89m }
        ];

        public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
        {
            logger.LogInformation("GetProductsQueryHandler.Handle llamado con {@Query}", query);

            try
            {
                IQueryable<Product> queryable = session.Query<Product>();

                if (!string.IsNullOrWhiteSpace(query.Name))
                    queryable = queryable.Where(p => p.Name.ToLower().Contains(query.Name.ToLower()));

                if (!string.IsNullOrWhiteSpace(query.Category))
                    queryable = queryable.Where(p => p.Category.Contains(query.Category));

                if (query.MinPrice.HasValue)
                    queryable = queryable.Where(p => p.Price >= query.MinPrice.Value);

                if (query.MaxPrice.HasValue)
                    queryable = queryable.Where(p => p.Price <= query.MaxPrice.Value);

                var totalCount = await queryable.CountAsync(cancellationToken);

                var products = await queryable
                    .OrderBy(p => p.Name)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(cancellationToken);

                var result = new PaginatedResult<Product>(query.PageIndex, query.PageSize, totalCount, products);
                return new GetProductsResult(result);
            }
            catch
            {
                var filtered = DemoProducts
                    .Where(p => string.IsNullOrWhiteSpace(query.Name) || p.Name.Contains(query.Name, StringComparison.OrdinalIgnoreCase))
                    .Where(p => string.IsNullOrWhiteSpace(query.Category) || p.Category.Contains(query.Category))
                    .Where(p => !query.MinPrice.HasValue || p.Price >= query.MinPrice.Value)
                    .Where(p => !query.MaxPrice.HasValue || p.Price <= query.MaxPrice.Value)
                    .OrderBy(p => p.Name)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                var result = new PaginatedResult<Product>(query.PageIndex, query.PageSize, DemoProducts.Count, filtered);
                return new GetProductsResult(result);
            }
        }
    }
}
