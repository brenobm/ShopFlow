using System.Globalization;
using System.Threading;
using CommunityToolkit.Maui.Media;

namespace BrenoMachado.ShopFlow.Services
{
    public class VoiceService : IVoiceService
    {
        private ISpeechToText _speechToText => SpeechToText.Default;

        public async Task<string> ListenAsync()
        {
            try
            {
                if (!await _speechToText.RequestPermissions(CancellationToken.None))
                {
                    await Shell.Current.DisplayAlert("Permission Denied", "Microphone permission is required.", "OK");
                    return null;
                }

                var progress = new Progress<string>(partialText => 
                {
                     // Optional: Could expose an event to update UI with partial text
                });

                var recognitionResult = await _speechToText.ListenAsync(
                    CultureInfo.CurrentCulture,
                    progress,
                    CancellationToken.None);

                if (recognitionResult.IsSuccessful)
                {
                    return recognitionResult.Text;
                }
                
                // Specific handling for common errors
                if (recognitionResult.Exception is TaskCanceledException)
                {
                    // Often happens on iOS if speech service is busy or user cancels
                    return null; 
                }

                await Shell.Current.DisplayAlert("Recognition Error", 
                    $"Error: {recognitionResult.Exception?.Message ?? "Unknown Error"}", "OK");
                return null;

                return null;
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
