using SQLite;
using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Services
{
    public class DatabaseService : IDatabaseService
    {
        SQLiteAsyncConnection? _database;

        async Task Init()
        {
            if (_database is not null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ShopFlow.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<ShoppingList>();
            await _database.CreateTableAsync<ShoppingItem>();
            await _database.CreateTableAsync<PurchaseHistory>();
            await _database.CreateTableAsync<CachedProduct>();
            await _database.CreateTableAsync<BarcodeMetric>();
        }

        public async Task<List<ShoppingList>> GetListsAsync()
        {
            await Init();
            return await _database.Table<ShoppingList>().OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<ShoppingList> GetListAsync(string id)
        {
            await Init();
            return await _database.Table<ShoppingList>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task SaveListAsync(ShoppingList list)
        {
            await Init();
            if (string.IsNullOrEmpty(list.Id))
            {
                list.Id = Guid.NewGuid().ToString();
                await _database.InsertAsync(list);
            }
            else
            {
                var existing = await GetListAsync(list.Id);
                if (existing == null)
                     await _database.InsertAsync(list);
                else
                     await _database.UpdateAsync(list);
            }
        }

        public async Task DeleteListAsync(ShoppingList list)
        {
            await Init();
            await _database.DeleteAsync(list);
            // Delete associated items
            var items = await GetItemsAsync(list.Id);
            foreach(var item in items)
            {
                await DeleteItemAsync(item);
            }
        }

        public async Task<List<ShoppingItem>> GetItemsAsync(string listId)
        {
            await Init();
            return await _database.Table<ShoppingItem>()
                            .Where(t => t.ListId == listId && !t.IsDeleted) // Filter out deleted
                            .OrderBy(x => x.IsPurchased)
                            .ThenBy(x => x.CreatedAt)
                            .ToListAsync();
        }

        public async Task<ShoppingItem> GetItemAsync(string itemId)
        {
            await Init();
            return await _database.Table<ShoppingItem>().Where(i => i.Id == itemId).FirstOrDefaultAsync();
        }

        public async Task SaveItemAsync(ShoppingItem item)
        {
            await Init();
            item.IsDirty = true; // Mark as Dirty
            item.LastUpdated = DateTime.UtcNow;

             if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = Guid.NewGuid().ToString();
                await _database.InsertAsync(item);
            }
            else
            {
                 var existing = await _database.Table<ShoppingItem>().Where(x => x.Id == item.Id).FirstOrDefaultAsync();
                if (existing == null)
                     await _database.InsertAsync(item);
                else
                     await _database.UpdateAsync(item);
            }
        }

        public async Task DeleteItemAsync(ShoppingItem item)
        {
            await Init();
            item.IsDeleted = true;
            item.IsDirty = true;
            item.LastUpdated = DateTime.UtcNow;
            await _database.UpdateAsync(item); // Soft Delete
        }
        
        public async Task<List<ShoppingItem>> GetDirtyItemsAsync()
        {
            await Init();
            return await _database.Table<ShoppingItem>().Where(i => i.IsDirty).ToListAsync();
        }

        public async Task<List<ShoppingList>> GetDirtyListsAsync()
        {
            await Init();
            return await _database.Table<ShoppingList>().Where(l => l.IsDirty).ToListAsync();
        }

        public async Task UpdateItemSyncStatusAsync(ShoppingItem item)
        {
            await Init();
            // Directly update the record without modifying IsDirty/LastUpdated logic
            await _database.UpdateAsync(item);
        }
        public async Task<List<ShoppingItem>> GetPurchasedItemsAsync()
        {
            await Init();
            return await _database.Table<ShoppingItem>().Where(i => i.IsPurchased).ToListAsync();
        }

        // Purchase History Methods
        public async Task LogPurchaseAsync(ShoppingItem item)
        {
            await Init();
            // Simple debounce: Don't log if purchased today already
            var today = DateTime.Today;
            var existing = await _database.Table<PurchaseHistory>()
                            .Where(x => x.ItemName == item.Name && x.DatePurchased >= today)
                            .FirstOrDefaultAsync();
            
            if (existing == null)
            {
                await _database.InsertAsync(new PurchaseHistory 
                { 
                    ItemName = item.Name, 
                    Category = item.Category ?? "Uncategorized",
                    DatePurchased = DateTime.Now 
                });
            }
        }

    public async Task<List<PurchaseHistory>> GetPurchaseHistoryAsync()
        {
            await Init();
            return await _database.Table<PurchaseHistory>().ToListAsync();
        }

        // Cached Products
        public async Task<CachedProduct> GetCachedProductAsync(string barcode)
        {
            await Init();
            return await _database.Table<CachedProduct>().Where(p => p.Barcode == barcode).FirstOrDefaultAsync();
        }

        public async Task SaveCachedProductAsync(CachedProduct product)
        {
            await Init();
            var existing = await GetCachedProductAsync(product.Barcode);
            if (existing == null)
            {
                await _database.InsertAsync(product);
            }
            else
            {
                await _database.UpdateAsync(product);
            }
        }
        
        public async Task<List<CachedProduct>> GetAllCachedProductsAsync()
        {
             await Init();
             return await _database.Table<CachedProduct>().ToListAsync();
        }

        // Metrics
        public async Task LogBarcodeSearchAsync(string barcode, bool found)
        {
            await Init();
            await _database.InsertAsync(new BarcodeMetric
            {
                Barcode = barcode,
                WasFoundOnline = found,
                DateSearched = DateTime.UtcNow
            });
        }
    }
}
