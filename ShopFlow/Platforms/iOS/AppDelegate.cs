using Foundation;
using Intents;
using UIKit;

using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Enable Audio Playback even in Silent Mode
        try
        {
            var session = AVFoundation.AVAudioSession.SharedInstance();
            session.SetCategory(AVFoundation.AVAudioSessionCategory.Playback);
            session.SetActive(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio Session Error: {ex.Message}");
        }

        // Request Siri Authorization
        Intents.INPreferences.RequestSiriAuthorization((status) =>
        {
            switch (status)
            {
                case Intents.INSiriAuthorizationStatus.Authorized:
                    FileLoggerService.LogStatic("Siri Authorized");
                    break;
                case Intents.INSiriAuthorizationStatus.Denied:
                    FileLoggerService.LogStatic("Siri Denied");
                    break;
                case Intents.INSiriAuthorizationStatus.NotDetermined:
                    FileLoggerService.LogStatic("Siri Not Determined");
                    break;
                case Intents.INSiriAuthorizationStatus.Restricted:
                    FileLoggerService.LogStatic("Siri Restricted");
                    break;
            }
        });

        return base.FinishedLaunching(application, launchOptions);
    }

    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        // Handle INAddTasksIntent (Siri "Add [milk]")
        // Note: Direct access to .Interaction might fail in some .NET iOS bindings, using KVC as workaround
        var interaction = userActivity.ValueForKey(new NSString("interaction")) as INInteraction;
        
        if (interaction?.Intent is INAddTasksIntent addIntent)
        {
             var items = addIntent.TaskTitles;
             if (items != null)
             {
                 foreach (var item in items)
                 {
                     var itemName = item.SpokenPhrase;
                     FileLoggerService.LogStatic($"Siri Request to Add: {itemName}");
                     
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
            FileLoggerService.LogStatic("Opened via Siri Shortcut");
            return true;
        }
        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

    // public override UIViewController? GetViewController(UIApplication application, string[] restorationIdentifierComponents, NSCoder coder) => base.GetViewController(application, restorationIdentifierComponents, coder);

    [Export("application:handlerForIntent:")]
    public NSObject GetHandlerForIntent(UIApplication application, INIntent intent)
    {
        if (intent is INAddTasksIntent)
        {
            return new BrenoMachado.ShopFlow.Platforms.iOS.AddTasksIntentHandler();
        }
        return null;
    }
    
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
