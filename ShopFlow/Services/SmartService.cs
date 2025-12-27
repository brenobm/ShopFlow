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
            var history = await _databaseService.GetPurchaseHistoryAsync();

            // Simple Frequency Logic: Top items by count
            // This is "Predictive" because we suggest what you buy most often
            // Ideally, we filters out items bought recently (replenishment logic), but let's stick to top frequent for now as per user request
            
            return history
                .GroupBy(i => i.ItemName)
                .OrderByDescending(g => g.Count())
                .Take(10) // Take top 10 candidates
                .Select(g => g.Key)
                .Take(5) // Limit to top 5 final suggestions
                .ToList();
        }
    }
}
