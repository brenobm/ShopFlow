using BrenoMachado.ShopFlow.Models;
using Microsoft.Extensions.Logging;

namespace BrenoMachado.ShopFlow.Services
{
    public class OpenFoodFactsService : IProductService
    {
        private readonly IGraphQLService _graphQLService;
        private readonly ILogger<OpenFoodFactsService> _logger;

        public OpenFoodFactsService(IGraphQLService graphQLService, ILogger<OpenFoodFactsService> logger)
        {
            _graphQLService = graphQLService;
            _logger = logger;
        }

        public async Task<ShoppingItem> LookupProductAsync(string barcode)
        {
            try
            {
                // Delegate to Backend for CloudWatch Logging
                var result = await _graphQLService.ProductSearchAsync(barcode);

                if (result != null)
                {
                    return new ShoppingItem
                    {
                        Name = result.Name,
                        Category = result.Category ?? "Groceries",
                        // ImageUrl could be added to ShoppingItem model if needed in future
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up product via Backend");
            }

            return null;
        }
    }
}
