using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Services
{
    public interface IOfflineSyncService
    {
        Task SyncOpenFoodFactsAsync();
    }

    public class OfflineSyncService : IOfflineSyncService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IProductService _productService;
        private const string LastSyncKey = "LastOFFSync";

        public OfflineSyncService(IDatabaseService databaseService, IProductService productService)
        {
            _databaseService = databaseService;
            _productService = productService;
        }

        public async Task SyncOpenFoodFactsAsync()
        {
            var lastSync = Preferences.Get(LastSyncKey, DateTime.MinValue);
            if (DateTime.UtcNow - lastSync < TimeSpan.FromDays(7))
            {
                return; // Less than 7 days
            }

            try
            {
                var cachedProducts = await _databaseService.GetAllCachedProductsAsync();
                foreach (var product in cachedProducts)
                {
                   // Debounce or rate limit? OFF API is free but be nice.
                   // maybe wait 100ms
                   await Task.Delay(100);

                   var updated = await _productService.LookupProductAsync(product.Barcode);
                   if (updated != null && !string.IsNullOrWhiteSpace(updated.Name))
                   {
                       if (updated.Name != product.Name || updated.Category != product.Category)
                       {
                           product.Name = updated.Name;
                           product.Category = updated.Category ?? product.Category;
                           product.LastUpdated = DateTime.UtcNow;
                           await _databaseService.SaveCachedProductAsync(product);
                       }
                   }
                }

                Preferences.Set(LastSyncKey, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"OFF Sync Failed: {ex.Message}");
            }
        }
    }
}
