using System.Globalization;
using BrenoMachado.ShopFlow.Resources.Strings;

namespace BrenoMachado.ShopFlow.Services;

public interface ILocalizationService
{
    CultureInfo GetCurrentCulture();
    void SetCulture(string cultureCode);
    void Initialize();
}

public class LocalizationService : ILocalizationService
{
    private const string LanguageKey = "AppLanguage";

    public LocalizationService()
    {
        Initialize();
    }

    public void Initialize()
    {
        string cultureCode = Preferences.Get(LanguageKey, "en-CA"); // Default to EN-CA
        SetCulture(cultureCode);
    }

    public CultureInfo GetCurrentCulture()
    {
        return Thread.CurrentThread.CurrentCulture;
    }

    public void SetCulture(string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);
            
            // Set for simple threads
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Save preference
            Preferences.Set(LanguageKey, cultureCode);
        }
        catch (Exception ex)
        {
            FileLoggerService.LogStatic($"Failed to set culture {cultureCode}: {ex.Message}");
        }
    }
}
