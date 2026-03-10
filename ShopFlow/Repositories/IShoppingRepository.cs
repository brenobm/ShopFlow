using System.Collections.Generic;
using System.Threading.Tasks;
using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Repositories
{
    public interface IShoppingRepository
    {
        Task<List<ShoppingList>> GetListsAsync();
        Task<ShoppingList> GetListAsync(string id);
        Task SaveListAsync(ShoppingList list);
        Task DeleteListAsync(ShoppingList list);
        Task<ShoppingList> ShareListAsync(string listId, string email);
        
        Task<List<ShoppingItem>> GetItemsAsync(string listId);
        Task<ShoppingItem> GetItemAsync(string itemId);
        Task SaveItemAsync(ShoppingItem item);
        Task DeleteItemAsync(ShoppingItem item);
        
        Task<List<PurchaseHistory>> GetPurchaseHistoryAsync();
    }
}
