using System.Net;
using System.Net.Http.Json;

namespace Order.API.Services
{
    public record BasketItemDto(int Quantity, string Color, decimal Price, Guid ProductId, string ProductName);
    public record BasketDto(string UserName, List<BasketItemDto> Items);
    public record GetBasketResponse(BasketDto Cart);

    public interface IBasketApiClient
    {
        Task<BasketDto?> GetBasketAsync(string userName, CancellationToken cancellationToken = default);
    }

    public class BasketApiClient(HttpClient httpClient) : IBasketApiClient
    {
        public async Task<BasketDto?> GetBasketAsync(string userName, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"/basket/{Uri.EscapeDataString(userName)}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<GetBasketResponse>(cancellationToken: cancellationToken);
            return payload?.Cart;
        }
    }
}