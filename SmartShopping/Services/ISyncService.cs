using System.Threading.Tasks;
using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Services
{
    public interface ISyncService
    {
        Task SyncAsync();
        Task PushItemAsync(Models.ShoppingItem item);
        Task DeleteItemAsync(string itemId);
    }
}
