namespace BrenoMachado.SmartShopping.Services
{
    public interface IVoiceService
    {
        Task<string> ListenAsync();
        void CancelSpeech();
    }
}
