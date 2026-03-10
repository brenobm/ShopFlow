using BrenoMachado.ShopFlow.Models;
using System.Diagnostics;

namespace BrenoMachado.ShopFlow.Services
{
    public class SyncService : ISyncService
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly IGraphQLService _graphQLService;

        public SyncService(IAuthService authService, IDatabaseService databaseService, IGraphQLService graphQLService)
        {
            _authService = authService;
            _databaseService = databaseService;
            _graphQLService = graphQLService;
        }

        public async Task SyncAsync()
        {
            await PushChangesAsync();
            await PullChangesAsync();
        }

        public async Task PullChangesAsync()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                FileLoggerService.LogStatic("[SyncService] Pull skipped: No Internet Connection.");
                return;
            }
            if (!await _authService.IsAuthenticatedAsync()) return;

            try
            {
                var lists = await _databaseService.GetListsAsync();
                foreach (var list in lists)
                {
                    var cloudItems = await _graphQLService.GetItemsAsync(list.Id);
                    
                    foreach (var cloudItem in cloudItems)
                    {
                        await ProcessCloudItemAsync(cloudItem);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"Sync Pull Error: {ex.Message}");
            }
        }

        private async Task ProcessCloudItemAsync(ShoppingItem cloudItem)
        {
             try 
             {
                 if (cloudItem == null || string.IsNullOrEmpty(cloudItem.Id)) return;

                 var localItem = await _databaseService.GetItemAsync(cloudItem.Id);
                 
                 // Conflict resolution
                 if (localItem != null)
                 {
                     if (localItem.IsDirty)
                     {
                          // Simple strategy: Local wins if dirty for now, as timestamps are unreliable.
                          // Ideally AppSync DeltaSync handles this natively, but to keep the app working offline-first:
                          return; 
                     }
                 }

                 cloudItem.IsDirty = false;
                 cloudItem.IsDeleted = false;
                 cloudItem.LastUpdated = DateTime.UtcNow;

                 await _databaseService.UpdateItemSyncStatusAsync(cloudItem);
             }
             catch(Exception ex)
             {
                 FileLoggerService.LogStatic($"Error processing cloud item: {ex.Message}");
             }
        }

        public async Task PushChangesAsync()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                FileLoggerService.LogStatic("[SyncService] Push skipped: No Internet Connection.");
                return;
            }
            if (!await _authService.IsAuthenticatedAsync()) return;
            
            try
            {
                var dirtyItems = await _databaseService.GetDirtyItemsAsync();
                if (!dirtyItems.Any()) return;

                foreach (var item in dirtyItems)
            {
                bool success = false;
                 try
                {
                    if (item.IsDeleted)
                    {
                        await DeleteItemOnCloudAsync(item);
                        success = true;
                    }
                    else
                    {
                        await PushItemToCloudAsync(item);
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    FileLoggerService.LogStatic($"Sync Push Error for {item.Name}: {ex.Message}");
                }

                    if (success)
                    {
                        item.IsDirty = false;
                        await _databaseService.UpdateItemSyncStatusAsync(item);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[SyncService] PushChanges Error: {ex.Message}");
            }
        }

        private async Task PushItemToCloudAsync(ShoppingItem item)
        {
             // Check if it already exists remotely and update, or add new
             // AppSync UpdateItem will fail if item doesn't exist due to condition `attribute_exists(id)`
             // Since we track locally, we can try Update, and if it fails (not found), Add it.
             // Or simpler: AddItem with ID handles creation! UpdateItem handles modifications.
             // Since we don't have a reliable `IsNew` flag on dirty items, we assume:
             // Try Update, catch -> Try Add. 
             try 
             {
                  await _graphQLService.UpdateItemAsync(item);
             }
             catch
             {
                  // Provide item.Id to AddItem to preserve correlation
                  await _graphQLService.AddItemAsync(
                      item.ListId, 
                      item.Id, 
                      item.Name, 
                      item.Quantity, 
                      item.Unit, 
                      item.Category, 
                      item.Price
                  );
                  // Push secondary fields like IsPurchased via subsequent update if needed, but AddItem sets to false initially.
                  if (item.IsPurchased)
                  {
                      await _graphQLService.UpdateItemAsync(item);
                  }
             }
             catch (Exception innerEx)
             {
                 FileLoggerService.LogStatic($"[SyncService] AddItemAsync failed after UpdateItemAsync failed: {innerEx.Message}");
                 throw; // Rethrow to mark as failed
             }
        }

        private async Task DeleteItemOnCloudAsync(ShoppingItem item)
        {
             await _graphQLService.DeleteItemAsync(item.ListId, item.Id);
        }

        public async Task DeleteItemAsync(string itemId)
        {
             // Not strictly needed anymore as PushChangesAsync handles dirty items,
             // but if we want instant delete:
             var item = await _databaseService.GetItemAsync(itemId);
             if (item != null)
             {
                 try
                 {
                     await _graphQLService.DeleteItemAsync(item.ListId, item.Id);
                 }
                 catch (Exception ex)
                 {
                     FileLoggerService.LogStatic($"DeleteItem Error: {ex.Message}");
                 }
             }
        }
    }
}
