using BrenoMachado.ShopFlow.Models;
using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.Services
{
    public interface IBarcodeWorkflowService
    {
        Task ExecuteScanWorkflowAsync(string listId);
    }
}
