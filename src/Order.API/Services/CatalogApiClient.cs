using System.Net;

namespace Order.API.Services
{
    public interface ICatalogApiClient
    {
        Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);
    }

    public class CatalogApiClient(HttpClient httpClient) : ICatalogApiClient
    {
        public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"/products/{productId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return false;

            response.EnsureSuccessStatusCode();
            return true;
        }
    }
}