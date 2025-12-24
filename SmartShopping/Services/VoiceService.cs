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
            try
            {
                var isGranted = await _speechToText.RequestPermissions(CancellationToken.None);
                if (!isGranted)
                {
                    await Shell.Current.DisplayAlert("Permission Denied", "Microphone permission is required for voice control. Please check your system settings.", "OK");
                    return null;
                }

#if IOS
                var audioSession = AVFoundation.AVAudioSession.SharedInstance();
                audioSession.SetCategory(AVFoundation.AVAudioSessionCategory.PlayAndRecord, AVFoundation.AVAudioSessionCategoryOptions.DefaultToSpeaker);
                audioSession.SetActive(true);
#endif

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
                else
                {
                    if (recognitionResult.Exception != null)
                    {
                         await Shell.Current.DisplayAlert("Recognition Error", $"Error: {recognitionResult.Exception.Message}", "OK");
                    }
                }

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
