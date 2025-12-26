using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Services
{
    public interface IGraphQLService
    {
        Task<List<ShoppingList>> GetListsAsync();
        Task<ShoppingList> CreateListAsync(string name);
        Task<ShoppingList> UpdateListAsync(string id, string name);
        Task<ShoppingList> DeleteListAsync(string id);
        Task<List<ShoppingItem>> GetItemsAsync(string listId); // Though typically fetched via List
        Task<ShoppingItem> AddItemAsync(string listId, string name, double quantity, string unit, string category);
        Task<ShoppingItem> UpdateItemAsync(ShoppingItem item);
        Task<ShoppingItem> DeleteItemAsync(string listId, string id);
    }
}
