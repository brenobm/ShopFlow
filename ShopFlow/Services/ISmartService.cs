using System.Collections.Generic;
using BrenoMachado.ShopFlow.Models;
using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.Services
{
    public interface ISmartService
    {
        Task<List<string>> GetSuggestionsAsync();
        (string Name, double Quantity, string Unit) ParseItemInput(string input);
        Task ProcessUnparsedItemsAsync(string listId);
    }
}
