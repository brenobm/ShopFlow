using System.Globalization;
using System.Threading;
using CommunityToolkit.Maui.Media;

namespace BrenoMachado.SmartShopping.Services
{
    public class VoiceService : IVoiceService
    {
        private ISpeechToText _speechToText => SpeechToText.Default;

        public async Task<string> ListenAsync()
        {
            var isGranted = await _speechToText.RequestPermissions(CancellationToken.None);
            if (!isGranted)
            {
                await Shell.Current.DisplayAlert("Permission Denied", "Microphone permission is required for voice control.", "OK");
                return null;
            }

            var recognitionResult = await _speechToText.ListenAsync(
                CultureInfo.CurrentCulture,
                new Progress<string>(partialText => 
                {
                    // Optional: handle partial results
                }), 
                CancellationToken.None);

            if (recognitionResult.IsSuccessful)
            {
                return recognitionResult.Text;
            }

            return null;
        }

        public void CancelSpeech()
        {
            // SpeechToText.Default does not support explicit disposal or cancellation via this API in current version
        }
    }
}
