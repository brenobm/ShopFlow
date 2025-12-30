namespace BrenoMachado.ShopFlow.Services
{
    public interface ILoggerService
    {
        void Log(string message);
        void LogError(string message, Exception ex = null);
        Task<string> GetLogsAsync();
        Task ClearLogsAsync();
    }
}
