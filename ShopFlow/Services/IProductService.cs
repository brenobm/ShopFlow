using BrenoMachado.ShopFlow.Models;

namespace BrenoMachado.ShopFlow.Services
{
    public interface IProductService
    {
        Task<ShoppingItem> LookupProductAsync(string barcode);
    }
}
