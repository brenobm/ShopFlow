using System.Text;

namespace BrenoMachado.ShopFlow.Services
{
    public class FileLoggerService : ILoggerService
    {
        private static string _logPath => Path.Combine(FileSystem.AppDataDirectory, "app_logs.txt");
        private static readonly object _lock = new object();

        // Static singleton for easy access in non-DI contexts (like AppDelegate)
        public static FileLoggerService Instance { get; private set; } = new FileLoggerService();

        public FileLoggerService()
        {
            if (Instance == null) Instance = this;
        }

        public void Log(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogError(string message, Exception ex = null)
        {
            var sb = new StringBuilder(message);
            if (ex != null)
            {
                sb.Append($" | Exception: {ex.Message}");
                sb.Append($" | Stack: {ex.StackTrace}");
            }
            WriteLog("ERROR", sb.ToString());
        }

        public Task<string> GetLogsAsync()
        {
            lock (_lock)
            {
                if (!File.Exists(_logPath)) return Task.FromResult(string.Empty);
                // Return last 100 lines if too big? For now, return all.
                try 
                {
                    return Task.FromResult(File.ReadAllText(_logPath));
                }
                catch
                {
                    return Task.FromResult("Error reading logs.");
                }
            }
        }

        public Task ClearLogsAsync()
        {
            lock (_lock)
            {
                if (File.Exists(_logPath)) File.Delete(_logPath);
            }
            return Task.CompletedTask;
        }

        private void WriteLog(string level, string message)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                
                // Console output as well for debugging while attached
                System.Diagnostics.Debug.Write(line);

                lock (_lock)
                {
                    File.AppendAllText(_logPath, line);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging Failed: {ex.Message}");
            }
        }
        
        // Static helper
        public static void LogStatic(string message) 
        {
            Instance.Log(message);
        }
    }
}
