namespace BrenoMachado.ShopFlow.Services
{
    public interface IVoiceService
    {
        Task<string> ListenAsync();
        void CancelSpeech();
    }
}
