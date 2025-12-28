using System;
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
                // TODO: Uncomment when Firebase works
                // await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                // var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                string token = "dummy-token-" + Guid.NewGuid().ToString(); // Mock

                if (!string.IsNullOrEmpty(token))
                {
                    string platform = DeviceInfo.Platform.ToString();
                    await _graphQLService.RegisterDeviceAsync(token, platform);
                    Console.WriteLine($"[PushService] Registered device token: {token}");
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PushService] Error registering for push: {ex.Message}");
            }
        }
    }
}
