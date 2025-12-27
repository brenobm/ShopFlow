namespace BrenoMachado.ShopFlow.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang);
    }
}
