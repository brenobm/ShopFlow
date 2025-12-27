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
                            .Where(t => t.ListId == listId)
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
            await _database.DeleteAsync(item);
        }
        public async Task<List<ShoppingItem>> GetPurchasedItemsAsync()
        {
            await Init();
            return await _database.Table<ShoppingItem>().Where(i => i.IsPurchased).ToListAsync();
        }

        // Purchase History Methods
        public async Task LogPurchaseAsync(string itemName)
        {
            await Init();
            // Simple debounce: Don't log if purchased today already
            var today = DateTime.Today;
            var existing = await _database.Table<PurchaseHistory>()
                            .Where(x => x.ItemName == itemName && x.DatePurchased >= today)
                            .FirstOrDefaultAsync();
            
            if (existing == null)
            {
                await _database.InsertAsync(new PurchaseHistory 
                { 
                    ItemName = itemName, 
                    DatePurchased = DateTime.Now 
                });
            }
        }

        public async Task<List<PurchaseHistory>> GetPurchaseHistoryAsync()
        {
            await Init();
            return await _database.Table<PurchaseHistory>().ToListAsync();
        }
    }
}
