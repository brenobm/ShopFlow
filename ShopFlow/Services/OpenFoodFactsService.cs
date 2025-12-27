using Newtonsoft.Json;
using BrenoMachado.ShopFlow.Models;
using Microsoft.Extensions.Logging;

namespace BrenoMachado.ShopFlow.Services
{
    public class OpenFoodFactsService : IProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenFoodFactsService> _logger;

        public OpenFoodFactsService(ILogger<OpenFoodFactsService> logger)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ShopFlow - Android - Version 1.0");
            _logger = logger;
        }

        public async Task<ShoppingItem> LookupProductAsync(string barcode)
        {
            try
            {
                var url = $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json";
                // Use GetStringAsync and Newtonsoft.Json to avoid System.Net.Http.Json missing reference issues
                var json = await _httpClient.GetStringAsync(url);
                var response = JsonConvert.DeserializeObject<OpenFoodFactsResponse>(json);

                if (response != null && response.Status == 1 && response.Product != null)
                {
                    return new ShoppingItem
                    {
                        Name = response.Product.ProductName,
                        // Could add image URL or brand if strictly needed, but ShoppingItem is simple for now
                        Category = "Groceries" // Default
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up product");
            }

            return null;
        }

        // Internal classes for JSON Deserialization
        private class OpenFoodFactsResponse
        {
            public int Status { get; set; }
            public ProductData Product { get; set; }
        }

        private class ProductData
        {
            public string ProductName { get; set; }
            public string Brands { get; set; }
            public string ImageUrl { get; set; }
        }
    }
}
