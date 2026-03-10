using System.Globalization;
using System.Threading;
using CommunityToolkit.Maui.Media;

namespace BrenoMachado.ShopFlow.Services
{
    using AppResources = BrenoMachado.ShopFlow.Resources.Strings.AppResources;
    public class VoiceService : IVoiceService
    {
        private ISpeechToText _speechToText => SpeechToText.Default;
        private readonly ILocalizationService _localizationService;
        private readonly ICrashReporterService _crashReporter;

        // Add Constructor for DI
        public VoiceService(ILocalizationService localizationService, ICrashReporterService crashReporter)
        {
            _localizationService = localizationService;
            _crashReporter = crashReporter;
        }
        
        public async Task<string> ListenAsync(CancellationToken cancellationToken)
        {
            FileLoggerService.LogStatic("VoiceService.ListenAsync Called");
            try
            {
                // Ensure we are on the Main Thread for iOS Speech Recognition
                return await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    FileLoggerService.LogStatic($"[VoiceService] Requesting Permissions... Platform: {DeviceInfo.Platform}");

                    // 1. Check Availability
                    bool isAvailable = await _speechToText.RequestPermissions(); // Some Toolkit versions use this
                    if (!isAvailable) 
                    {
                         FileLoggerService.LogStatic("[VoiceService] SpeechToText Not Available or Permission Denied via internally.");
                    }
                    
                    var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
                    FileLoggerService.LogStatic($"[VoiceService] Mic Status: {micStatus}");
                    
                    if (micStatus != PermissionStatus.Granted)
                    {
                         await Shell.Current.DisplayAlert(AppResources.Msg_PermissionDenied, AppResources.Msg_MicRequired, "OK");
                         return null;
                    }

                    // Explicitly request Speech permission on iOS
                    if (DeviceInfo.Platform == DevicePlatform.iOS)
                    {
                        var speechStatus = await Permissions.RequestAsync<Permissions.Speech>();
                        FileLoggerService.LogStatic($"[VoiceService] Speech Status: {speechStatus}");
                        
                        if (speechStatus != PermissionStatus.Granted)
                        {
                            await Shell.Current.DisplayAlert(AppResources.Msg_PermissionDenied, AppResources.Msg_SpeechRequired, "OK");
                            return null;
                        }
                    }
                    
                    // 2. Extra Check for Simulator
                    if (DeviceInfo.DeviceType == DeviceType.Virtual && DeviceInfo.Platform == DevicePlatform.iOS)
                    {
                         FileLoggerService.LogStatic("[VoiceService] Warning: Running on iOS Simulator. dictation may fail if not enabled in Host Mac.");
                    }

                // Capture partial results to return in case of cancellation/timeout
                string capturedText = "";
                var progress = new Progress<string>(partialText => 
                {
                     capturedText = partialText;
                     System.Diagnostics.Debug.WriteLine($"Partial: {partialText}");
                });

                FileLoggerService.LogStatic($"Starting ListenAsync with culture: {_localizationService.GetCurrentCulture().Name}");
                
                SpeechToTextResult recognitionResult = null;
                try 
                {
                    recognitionResult = await _speechToText.ListenAsync(
                        _localizationService.GetCurrentCulture(),
                        progress,
                        cancellationToken);
                }
                catch(TaskCanceledException)
                {
                     // User released button -> Stop. Return what we heard.
                     FileLoggerService.LogStatic($"ListenAsync Cancelled. Returning partial: '{capturedText}'");
                     return capturedText;
                }

                FileLoggerService.LogStatic($"Recognition Result: {recognitionResult?.IsSuccessful} - {recognitionResult?.Text}");
                
                if (recognitionResult != null && recognitionResult.IsSuccessful && !string.IsNullOrWhiteSpace(recognitionResult.Text))
                {
                    return recognitionResult.Text;
                }
                
                return capturedText;
            });

            }
            catch (Exception ex)
            {
                // TaskCanceledException is expected here on "Stop"
                if (ex is TaskCanceledException) return null;

                _crashReporter.ReportExceptionAsync(ex, "VoiceService ListenAsync");
                await Shell.Current.DisplayAlert(AppResources.Error, string.Format(AppResources.Msg_VoiceError, ex.Message), "OK");
                return null;
            }
        }

        public void CancelSpeech()
        {
            // SpeechToText.Default does not support explicit disposal or cancellation via this API in current version
        }
    }
}
