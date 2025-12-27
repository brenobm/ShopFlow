using Foundation;
using Intents;
using UIKit;

namespace BrenoMachado.ShopFlow;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Request Siri Authorization
        Intents.INPreferences.RequestSiriAuthorization((status) =>
        {
            switch (status)
            {
                case Intents.INSiriAuthorizationStatus.Authorized:
                    Console.WriteLine("Siri Authorized");
                    break;
                case Intents.INSiriAuthorizationStatus.Denied:
                    Console.WriteLine("Siri Denied");
                    break;
                case Intents.INSiriAuthorizationStatus.NotDetermined:
                    Console.WriteLine("Siri Not Determined");
                    break;
                case Intents.INSiriAuthorizationStatus.Restricted:
                    Console.WriteLine("Siri Restricted");
                    break;
            }
        });

        return base.FinishedLaunching(application, launchOptions);
    }

    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        if (userActivity.ActivityType == "com.brenomachado.shopflow.additem")
        {
            // Handle the shortcut/intent here
            // For now, we just log it. In a real scenario, we'd navigate to the page.
            Console.WriteLine("Opened via Siri Shortcut");
            return true;
        }
        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
