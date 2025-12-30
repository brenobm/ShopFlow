using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Services
{
    public interface IGraphQLService
    {
        Task<List<ShoppingList>> GetListsAsync();
        Task<ShoppingList> CreateListAsync(string name, bool isDefault = false);
        Task<ShoppingList> UpdateListAsync(string id, string name, bool isDefault);
        Task<ShoppingList> DeleteListAsync(string id);
        Task<ShoppingList> ShareListAsync(string id, string email);
        Task<List<ShoppingItem>> GetItemsAsync(string listId); // Though typically fetched via List
        Task<ShoppingItem> AddItemAsync(string listId, string name, double quantity, string unit, string category, double? price = null);
        Task<ShoppingItem> UpdateItemAsync(ShoppingItem item);
        Task<ShoppingItem> DeleteItemAsync(string listId, string id);
        Task<GlobalProduct> GetGlobalProductAsync(string barcode);
        Task<GlobalProduct> CreateGlobalProductAsync(string barcode, string name, string category);
        Task<ProductSearchDto> ProductSearchAsync(string barcode);
        Task RegisterDeviceAsync(string token, string platform);
    }
}
