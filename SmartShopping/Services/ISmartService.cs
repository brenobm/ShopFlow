using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrenoMachado.SmartShopping.Services
{
    public interface ISmartService
    {
        Task<List<string>> GetSuggestionsAsync();
    }
}
