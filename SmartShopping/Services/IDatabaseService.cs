using BrenoMachado.SmartShopping.Models;

namespace BrenoMachado.SmartShopping.Services
{
    public interface IDatabaseService
    {
        Task<List<ShoppingList>> GetListsAsync();
        Task<ShoppingList> GetListAsync(string id);
        Task SaveListAsync(ShoppingList list);
        Task DeleteListAsync(ShoppingList list);
        
        Task<List<ShoppingItem>> GetItemsAsync(string listId);
        Task<ShoppingItem> GetItemAsync(string itemId);
        Task SaveItemAsync(ShoppingItem item);
        Task DeleteItemAsync(ShoppingItem item);
        
        Task<List<ShoppingItem>> GetPurchasedItemsAsync();
    }
}
