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
        // Handle INAddTasksIntent (Siri "Add [milk]")
        if (userActivity.Interaction?.Intent is INAddTasksIntent addIntent)
        {
             var items = addIntent.TaskTitles;
             if (items != null)
             {
                 foreach (var item in items)
                 {
                     var itemName = item.SpokenPhrase;
                     Console.WriteLine($"Siri Request to Add: {itemName}");
                     
                     // Dispatch to main thread to navigate/add
                     MainThread.BeginInvokeOnMainThread(async () => 
                     {
                         // Navigate to ActiveList and Add Item
                         await Shell.Current.GoToAsync($"//ActiveListPage?action=add&item={itemName}");
                     });
                 }
             }
             return true;
        }

        if (userActivity.ActivityType == "com.brenomachado.shopflow.additem")
        {
            Console.WriteLine("Opened via Siri Shortcut");
            return true;
        }
        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
