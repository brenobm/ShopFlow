using System.Collections.Generic;
using System.Linq;
using BrenoMachado.ShopFlow.Models;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using BrenoMachado.ShopFlow.Messages;

namespace BrenoMachado.ShopFlow.Services
{
    public class SmartService : ISmartService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IGraphQLService _graphQLService;

        public SmartService(IDatabaseService databaseService, IGraphQLService graphQLService)
        {
            _databaseService = databaseService;
            _graphQLService = graphQLService;
        }

        public async Task<List<string>> GetSuggestionsAsync()
        {
            var history = await _databaseService.GetPurchaseHistoryAsync();
            return history
                .GroupBy(i => i.ItemName)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .Take(5)
                .ToList();
        }

        public (string Name, double Quantity, string Unit) ParseItemInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return (input, 1, "qty");
            
            // Units Pattern (Same as before)
            string unitsPattern = @"kg|g|mg|l|ml|oz|lbs?|pounds?|ounces?|oncas?|onças?|grams?|gramas?|kilos?|quilos?|kilograms?|quilogramas?|pcs|units?|un|unidades?|pack(?:s)?|pacote(?:s)?|box(?:es)?|caixa(?:s)?|bag(?:s)?|sacola(?:s)?|litres?|litros?|liters?|milliliters?|mililitros?|gallons?|galoes?|galões?|pints?|cups?|xicaras?|xícaras?|cans?|latas?|bottles?|garrafas?|jars?|potes?|slices?|fatias?|bunch|macos?|dozens?|duzias?";

            // 1. Try Prefix Pattern
            var prefixRegex = new System.Text.RegularExpressions.Regex($@"^(\d+(?:\.\d+)?)\s*({unitsPattern})?\s+(?:(of|de)\s+)?(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var matchPrefix = prefixRegex.Match(input.Trim());

            if (matchPrefix.Success)
            {
                double.TryParse(matchPrefix.Groups[1].Value, out double qty);
                string unit = matchPrefix.Groups[2].Value.ToLower();
                string name = matchPrefix.Groups[4].Value.Trim();
                return (name, qty, NormalizeUnit(unit));
            }

            // 2. Try Suffix Pattern
            var suffixRegex = new System.Text.RegularExpressions.Regex($@"^(.+?)\s+(\d+(?:\.\d+)?)\s*({unitsPattern})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var matchSuffix = suffixRegex.Match(input.Trim());

            if (matchSuffix.Success)
            {
                 string name = matchSuffix.Groups[1].Value.Trim();
                 double.TryParse(matchSuffix.Groups[2].Value, out double qty);
                 string unit = matchSuffix.Groups[3].Value.ToLower();
                 return (name, qty, NormalizeUnit(unit));
            }

            return (input.Trim(), 1, "qty");
        }

        public async Task ProcessUnparsedItemsAsync(string listId)
        {
            FileLoggerService.LogStatic($"[SmartService] ProcessUnparsedItemsAsync called for ListId: {listId}");

            var allItems = await _databaseService.GetItemsAsync(listId);
            var unparsedItems = allItems.Where(i => !i.IsParsed).ToList();

            if (!unparsedItems.Any()) return;

            if (Connectivity.NetworkAccess != NetworkAccess.Internet) 
            {
                FileLoggerService.LogStatic("[SmartService] No internet connection. Skipping parsing.");
                return;
            }

            FileLoggerService.LogStatic($"[SmartService] Found {unparsedItems.Count} unparsed items. Starting processing...");

            foreach (var item in unparsedItems)
            {
                try
                {
                    // Call Backend via GraphQL
                    var result = await _graphQLService.ParseItemAsync(item.OriginalText);
                    
                    if (result != null)
                    {
                        item.Name = result.Name;
                        item.Quantity = result.Quantity;
                        item.Unit = NormalizeUnit(result.Unit);
                        
                        // Use Category from Backend
                        item.Category = result.Category; 
                        
                        // We could use Emoji if we had a field, but for now we trust the category mapping or just the name
                        
                        item.IsParsed = true;
                        
                        await _databaseService.SaveItemAsync(item);
                        WeakReferenceMessenger.Default.Send(new ItemParsedMessage(listId));
                    }
                }
                catch (Exception ex)
                {
                    FileLoggerService.LogStatic($"Backend Parse Error: {ex.Message}");
                }
            }
        }

        private string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return "qty";
            
            unit = unit.ToLower().Trim();
            
            // Standardize 
            if (unit.StartsWith("pack") || unit.StartsWith("pacote")) return "pack";
            if (unit.StartsWith("box") || unit.StartsWith("caixa")) return "box";
            if (unit.StartsWith("bag") || unit.StartsWith("sacola")) return "bag";
            
            // Weight
            if (unit.StartsWith("lb") || unit.StartsWith("pound")) return "lb";
            if (unit.StartsWith("oz") || unit.StartsWith("onca") || unit.StartsWith("onça") || unit.StartsWith("ounce")) return "oz";
            if (unit.StartsWith("kg") || unit.StartsWith("kilo") || unit.StartsWith("quilo") || unit.StartsWith("kilogram") || unit.StartsWith("quilograma")) return "kg";
            if (unit == "g" || unit.StartsWith("gram")) return "g";
            if (unit == "mg") return "mg";

            // Volume
            if (unit.StartsWith("litr") || unit.StartsWith("liter")) return "l";
            if (unit.StartsWith("milliliter") || unit.StartsWith("mililitro")) return "ml";
            if (unit.StartsWith("gallon") || unit.StartsWith("gal")) return "gal";
            if (unit == "cup" || unit.StartsWith("cup") || unit.StartsWith("xicara") || unit.StartsWith("xícara")) return "cup";
            if (unit.StartsWith("pint")) return "pt";
            
            if (unit.StartsWith("can") || unit.StartsWith("lata")) return "can";
            if (unit.StartsWith("bottle") || unit.StartsWith("garrafa")) return "bottle";
            if (unit.StartsWith("jar") || unit.StartsWith("pote")) return "jar";
            if (unit.StartsWith("slice") || unit.StartsWith("fatia")) return "slice";
            if (unit.StartsWith("bunch") || unit.StartsWith("maco") || unit.StartsWith("maço")) return "bunch";
            if (unit.StartsWith("dozen") || unit.StartsWith("duzia") || unit.StartsWith("dúzia")) return "dz";
            if (unit.StartsWith("unit") || unit.StartsWith("unidade") || unit == "pcs" || unit == "pc") return "un";

            return unit;
        }
    }
}
