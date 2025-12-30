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

        public (string Name, double Quantity, string Unit) ParseItemInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return (input, 1, "qty");
            
            // Regex to find "Number" + "Unit" + "Name"
            // Examples: "2kg Rice", "500ml Milk", "6 Bananas", "2 packs of soda"
            // Pattern:
            // ^(\d+(?:\.\d+)?)   -> Start with Number (integer or decimal)
            // \s*                -> Optional space
            // (kg|g|l|ml|oz|lb|pcs|pack|box|bag)? -> Optional Unit
            // \s+                -> Required space
            // (?:of\s+)?         -> Optional "of"
            // (.+)$              -> The rest is the Name
            
            var regex = new System.Text.RegularExpressions.Regex(@"^(\d+(?:\.\d+)?)\s*(kg|g|l|ml|oz|lbs?|pcs|pack(?:s)?|box(?:es)?|bag(?:s)?)?\s+(?:of\s+)?(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var match = regex.Match(input.Trim());

            if (match.Success)
            {
                double.TryParse(match.Groups[1].Value, out double qty);
                string unit = match.Groups[2].Value.ToLower();
                string name = match.Groups[3].Value.Trim();

                // Normalize Units
                if (string.IsNullOrWhiteSpace(unit)) unit = "qty";
                else if (unit.StartsWith("pack")) unit = "pack";
                else if (unit.StartsWith("box")) unit = "box";
                else if (unit.StartsWith("bag")) unit = "bag";
                else if (unit.StartsWith("lb")) unit = "lb";

                return (name, qty, unit);
            }

            // Fallback: No qty detected
            return (input.Trim(), 1, "qty");
        }
    }
}
