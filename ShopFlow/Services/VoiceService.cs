using System.Globalization;
using System.Threading;
using CommunityToolkit.Maui.Media;

namespace BrenoMachado.ShopFlow.Services
{
    public class VoiceService : IVoiceService
    {
        private ISpeechToText _speechToText => SpeechToText.Default;
        private readonly ILocalizationService _localizationService;

        // Add Constructor for DI
        public VoiceService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }
        
        // Add Constructor for backward compatibility if needed, but safer to force DI.
        // Or if DI fails, fallback? No, we registered it.
        
        public async Task<string> ListenAsync()
        {
            System.Diagnostics.Debug.WriteLine("VoiceService.ListenAsync Called");
            try
            {
                // Ensure we are on the Main Thread for iOS Speech Recognition
                return await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    System.Diagnostics.Debug.WriteLine("Requesting Permissions...");
                    
                    var micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (micStatus != PermissionStatus.Granted)
                    {
                         await Shell.Current.DisplayAlert("Permission Denied", "Microphone permission is required.", "OK");
                         return null;
                    }

                    // Explicitly request Speech permission on iOS
                    if (DeviceInfo.Platform == DevicePlatform.iOS)
                    {
                        var speechStatus = await Permissions.RequestAsync<Permissions.Speech>();
                        if (speechStatus != PermissionStatus.Granted)
                        {
                            await Shell.Current.DisplayAlert("Permission Denied", "Speech Recognition permission is required.", "OK");
                            return null;
                        }
                    }

                    var progress = new Progress<string>(partialText => 
                    {
                         System.Diagnostics.Debug.WriteLine($"Partial: {partialText}");
                    });

                    System.Diagnostics.Debug.WriteLine($"Starting ListenAsync with culture: {_localizationService.GetCurrentCulture().Name}");
                    
                    var recognitionResult = await _speechToText.ListenAsync(
                        _localizationService.GetCurrentCulture(),
                        progress,
                        CancellationToken.None);
                    
                    System.Diagnostics.Debug.WriteLine($"Recognition Result: {recognitionResult.IsSuccessful} - {recognitionResult.Text}");
                    return recognitionResult.IsSuccessful ? recognitionResult.Text : null;
                });

                 /* Logic moved inside MainThread block */
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Voice Error", $"An internal error occurred: {ex.Message}", "OK");
                return null;
            }
        }

        public void CancelSpeech()
        {
            // SpeechToText.Default does not support explicit disposal or cancellation via this API in current version
        }
    }
}
