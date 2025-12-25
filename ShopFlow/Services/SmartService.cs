using System.Collections.Generic;
using System.Linq;
using BrenoMachado.ShopFlow.Models;
using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.Services
{
    public class SmartService : ISmartService
    {
        private readonly IDatabaseService _databaseService;

        public SmartService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<string>> GetSuggestionsAsync()
        {
            var purchasedItems = await _databaseService.GetPurchasedItemsAsync();
            return purchasedItems
                .GroupBy(i => i.Name)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();
        }
    }
}
