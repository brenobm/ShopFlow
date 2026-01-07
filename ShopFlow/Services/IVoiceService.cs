namespace BrenoMachado.ShopFlow.Services
{
    public interface IVoiceService
    {
        Task<string> ListenAsync(CancellationToken cancellationToken);
        void CancelSpeech();
    }
}
