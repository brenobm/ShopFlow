using Plugin.LocalNotification;
// #if ANDROID
// using Plugin.Firebase.CloudMessaging;
// #endif

namespace BrenoMachado.ShopFlow.Services
{
    public interface IAppNotificationService
    {
        Task InitializeAsync();
        Task ShowLocalNotification(string title, string message);
        Task<string> GetDeviceTokenAsync();
    }

    public class NotificationService : IAppNotificationService
    {
        public async Task InitializeAsync()
        {
            // Initialize Local Notifications
            // await LocalNotificationCenter.Current.RequestNotificationPermission(); // Handled by MauiProgram usually or explicit call
            
            // Remote Push Initialization would go here if not auto-init
        }

        public async Task ShowLocalNotification(string title, string message)
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }

            var request = new NotificationRequest
            {
                NotificationId = new Random().Next(1000, 9999),
                Title = title,
                Description = message,
                BadgeNumber = 1,
                Schedule = { NotifyTime = DateTime.Now.AddSeconds(1) }
            };
            
            await LocalNotificationCenter.Current.Show(request);
        }

        public async Task<string> GetDeviceTokenAsync()
        {
#if ANDROID
             // return await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
             // Commented out to prevent build errors until google-services.json is present
             return "dummy-token"; 
#else
             return "ios-token-placeholder";
#endif
        }
    }
}
