#if ANDROID || IOS
using Plugin.Firebase.CloudMessaging;
#endif
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace BrenoMachado.ShopFlow.Services
{
    public class PushService : IPushService
    {
        private readonly IGraphQLService _graphQLService;

        public PushService(IGraphQLService graphQLService)
        {
            _graphQLService = graphQLService;
        }

        public async Task RegisterForPushNotificationsAsync()
        {
            try
            {
                // Basic cross-platform check (Plugin.Firebase handles checking availability internally mostly)
#if ANDROID || IOS
                try
                {
                    await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                    var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        string platform = DeviceInfo.Platform.ToString();
                        await _graphQLService.RegisterDeviceAsync(token, platform);
                        FileLoggerService.LogStatic($"[PushService] Registered device token: {token}");
                    }
                }
                catch (Exception ex)
                {
                     FileLoggerService.LogStatic($"[PushService] Firebase Token Error: {ex.Message}");
                }
#endif
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[PushService] Error registering for push: {ex.Message}");
            }
        }
    }
}
