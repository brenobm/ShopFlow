using System.Threading.Tasks;
using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Services
{
    public interface ISyncService
    {
        Task SyncAsync();
        Task PushChangesAsync();
        Task PullChangesAsync();
        Task DeleteItemAsync(string itemId);
    }
}
