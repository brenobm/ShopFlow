using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;
using System.Diagnostics;

namespace BrenoMachado.ShopFlow.Repositories
{
    public interface IShoppingRepository
    {
        Task<List<ShoppingItem>> GetItemsAsync(string listId);
        Task SaveItemAsync(ShoppingItem item);
        Task DeleteItemAsync(ShoppingItem item);
        Task SyncAsync();
    }

    public class ShoppingRepository : IShoppingRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ISyncService _syncService;
        private readonly ISmartService _smartService;

        public ShoppingRepository(IDatabaseService databaseService, ISyncService syncService, ISmartService smartService)
        {
            _databaseService = databaseService;
            _syncService = syncService;
            _smartService = smartService;

            // Auto-Sync when connectivity returns
            Connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                FileLoggerService.LogStatic("Connectivity Restored: Triggering Sync");
                FireAndForgetSync();
                
                // Trigger Smart Parsing for unparsed items
                 Task.Run(async () => {
                     try {
                        var lists = await _databaseService.GetListsAsync();
                        foreach(var list in lists)
                        {
                            await _smartService.ProcessUnparsedItemsAsync(list.Id);
                        }
                     } catch(Exception ex) {
                         System.Diagnostics.Debug.WriteLine($"Smart Sync Failed: {ex.Message}");
                     }
                 });
            }
        }

        public async Task<List<ShoppingItem>> GetItemsAsync(string listId)
        {
            // Always read from local DB for speed and offline handling
            return await _databaseService.GetItemsAsync(listId);
        }

        public async Task SaveItemAsync(ShoppingItem item)
        {
            // 1. Save Local (Immediate UI update capability)
            await _databaseService.SaveItemAsync(item);

            // 2. Trigger Sync (Fire and Forget)
            FireAndForgetSync();
        }

        public async Task DeleteItemAsync(ShoppingItem item)
        {
            // 1. Soft Delete Local
            await _databaseService.DeleteItemAsync(item);

            // 2. Trigger Sync
            FireAndForgetSync();
        }

        public async Task SyncAsync()
        {
            // Manual full sync logic
            try
            {
               await _syncService.SyncAsync();
            } 
            catch (Exception ex)
            {
                Debug.WriteLine($"Manual Sync Failed: {ex.Message}");
            }
        }

        private void FireAndForgetSync()
        {
            // Run on background thread to not block UI
            Task.Run(async () => 
            {
                try
                {
                    await _syncService.PushChangesAsync();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Background Sync Failed: {ex.Message}");
                }
            });
        }
    }
}
