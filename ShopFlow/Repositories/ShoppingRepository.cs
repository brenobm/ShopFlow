using System.Collections.Generic;
using System.Threading.Tasks;
using BrenoMachado.ShopFlow.Models;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow.Repositories
{
    public class ShoppingRepository : IShoppingRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly IGraphQLService _graphQLService;

        public ShoppingRepository(IDatabaseService databaseService, IGraphQLService graphQLService)
        {
            _databaseService = databaseService;
            _graphQLService = graphQLService;
        }

        public Task<List<ShoppingList>> GetListsAsync()
        {
            return _databaseService.GetListsAsync();
        }

        public Task<ShoppingList> GetListAsync(string id)
        {
            return _databaseService.GetListAsync(id);
        }

        public Task SaveListAsync(ShoppingList list)
        {
            return _databaseService.SaveListAsync(list);
        }

        public Task DeleteListAsync(ShoppingList list)
        {
            return _databaseService.DeleteListAsync(list);
        }

        public Task<ShoppingList> ShareListAsync(string listId, string email)
        {
            return _graphQLService.ShareListAsync(listId, email);
        }

        public Task<List<ShoppingItem>> GetItemsAsync(string listId)
        {
            return _databaseService.GetItemsAsync(listId);
        }

        public Task<ShoppingItem> GetItemAsync(string itemId)
        {
            return _databaseService.GetItemAsync(itemId);
        }

        public Task SaveItemAsync(ShoppingItem item)
        {
            return _databaseService.SaveItemAsync(item);
        }

        public Task DeleteItemAsync(ShoppingItem item)
        {
            return _databaseService.DeleteItemAsync(item);
        }

        public Task<List<PurchaseHistory>> GetPurchaseHistoryAsync()
        {
            return _databaseService.GetPurchaseHistoryAsync();
        }
    }
}
