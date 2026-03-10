using System;
using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.Services
{
    public class CrashReporterService : ICrashReporterService
    {
        private readonly ILoggerService _loggerService;

        public CrashReporterService(ILoggerService loggerService)
        {
            _loggerService = loggerService;
        }

        public async Task ReportExceptionAsync(Exception exception, string context = "")
        {
            if (exception == null) return;

            string errorMessage = $"[CRASH/ERROR] Context: {context} | Message: {exception.Message} | StackTrace: {exception.StackTrace}";
            
            // Local fallback
            _loggerService.Log(errorMessage);
            FileLoggerService.LogStatic(errorMessage);

            // TODO: Here we would send to AppCenter, Firebase Crashlytics, or a custom AWS HTTP endpoint.
            // Since this project relies purely on built-in MAUI/AWS capabilities without 3rd party SDKs currently working,
            // we proxy it through a safe Task.
            
            await Task.Run(() => 
            {
                // Telemetry transmission simulation
                System.Diagnostics.Debug.WriteLine($"Reported to Cloud: {errorMessage}");
            });
        }
    }
}
