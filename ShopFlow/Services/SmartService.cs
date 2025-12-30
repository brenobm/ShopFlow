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
            
            // Units Pattern (Expanded): 
            // - Weight: kg, g, mg, lb(s), oz, pound(s), ounce(s), gram(s), kilo(s)
            // - PT Weight: grama(s), quilo(s), quilograma(s), onça(s)
            // - Volume: l, ml, litre(s), litro(s), liter(s), milliliter(s), mililitro(s), gallon(s), galao(s), pint(s), cup(s), xicara(s)
            // - Count: pcs, unit(s), un, unidade(s)
            // - Container: pack(s), box(es), bag(s), can(s), bottle(s), jar(s)
            // - PT Containers: pacote(s), caixa(s), sacola(s), lata(s), garrafa(s), pote(s)
            // - Other: dozen(s), duzia(s), bunch, maco(s)
            
            string unitsPattern = @"kg|g|mg|l|ml|oz|lbs?|pounds?|ounces?|oncas?|onças?|grams?|gramas?|kilos?|quilos?|kilograms?|quilogramas?|pcs|units?|un|unidades?|pack(?:s)?|pacote(?:s)?|box(?:es)?|caixa(?:s)?|bag(?:s)?|sacola(?:s)?|litres?|litros?|liters?|milliliters?|mililitros?|gallons?|galoes?|galões?|pints?|cups?|xicaras?|xícaras?|cans?|latas?|bottles?|garrafas?|jars?|potes?|slices?|fatias?|bunch|macos?|dozens?|duzias?";

            // 1. Try Prefix Pattern: "2kg Rice", "1 litro de leite", "1 can of beans"
            var prefixRegex = new System.Text.RegularExpressions.Regex($@"^(\d+(?:\.\d+)?)\s*({unitsPattern})?\s+(?:(of|de)\s+)?(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var matchPrefix = prefixRegex.Match(input.Trim());

            if (matchPrefix.Success)
            {
                double.TryParse(matchPrefix.Groups[1].Value, out double qty);
                string unit = matchPrefix.Groups[2].Value.ToLower();
                string name = matchPrefix.Groups[4].Value.Trim();
                return (name, qty, NormalizeUnit(unit));
            }

            // 2. Try Suffix Pattern: "Rice 2kg", "Leite 1 litro", "Beans 2 cans"
            var suffixRegex = new System.Text.RegularExpressions.Regex($@"^(.+?)\s+(\d+(?:\.\d+)?)\s*({unitsPattern})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var matchSuffix = suffixRegex.Match(input.Trim());

            if (matchSuffix.Success)
            {
                 string name = matchSuffix.Groups[1].Value.Trim();
                 double.TryParse(matchSuffix.Groups[2].Value, out double qty);
                 string unit = matchSuffix.Groups[3].Value.ToLower();
                 return (name, qty, NormalizeUnit(unit));
            }

            // Fallback: No qty detected
            return (input.Trim(), 1, "qty");
        }

        private string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return "qty";
            
            unit = unit.ToLower().Trim();
            
            // Standardize to common short codes where possible
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
