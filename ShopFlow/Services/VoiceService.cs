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
                // Ensure we are on the Main Thread for iOS Speech Recognition
                return await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    if (!await _speechToText.RequestPermissions(CancellationToken.None))
                    {
                        await Shell.Current.DisplayAlert("Permission Denied", "Microphone permission is required.", "OK");
                        return null;
                    }

                    var progress = new Progress<string>(partialText => 
                    {
                         // Optional: UI updates
                    });

                    var recognitionResult = await _speechToText.ListenAsync(
                        CultureInfo.CurrentCulture,
                        progress,
                        CancellationToken.None);
                    
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
