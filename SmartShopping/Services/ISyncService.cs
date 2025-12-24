using System.Threading.Tasks;

namespace BrenoMachado.SmartShopping.Services
{
    public interface ISyncService
    {
        Task SyncAsync();
        Task PushItemAsync(Models.ShoppingItem item);
        Task DeleteItemAsync(string itemId);
    }
}
