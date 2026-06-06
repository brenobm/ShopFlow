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
        // Enable Audio Playback even in Silent Mode (required for voice recognition)
        try
        {
            var session = AVFoundation.AVAudioSession.SharedInstance();
            session.SetCategory(AVFoundation.AVAudioSessionCategory.PlayAndRecord,
                                AVFoundation.AVAudioSessionCategoryOptions.DefaultToSpeaker | AVFoundation.AVAudioSessionCategoryOptions.MixWithOthers);
            session.SetActive(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio Session Error: {ex.Message}");
        }

        // Request Siri Authorization, then donate our shortcut suggestion so Siri
        // knows ShopFlow can handle add-item commands.
        INPreferences.RequestSiriAuthorization((status) =>
        {
            FileLoggerService.LogStatic($"[Siri] Authorization status: {status}");

            if (status == INSiriAuthorizationStatus.Authorized)
            {
                // Donate the shortcut suggestion so it appears in
                // Settings > Siri & Search > ShopFlow and Siri learns about us.
                var siriService = IPlatformApplication.Current?.Services?.GetService<ISiriService>();
                siriService?.DonateAddItemShortcut();
            }
        });

        // Initialize Firebase
        Firebase.Core.App.Configure();

        return base.FinishedLaunching(application, launchOptions);
    }

    // Called when the app is launched/foregrounded to handle a Siri Shortcut
    // (NSUserActivity-based shortcuts, e.g. com.brenomachado.shopflow.additem).
    // NOTE: INAddTasksIntent is NOT handled here — it's routed via GetHandlerForIntent below.
    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        if (userActivity.ActivityType == "com.brenomachado.shopflow.additem")
        {
            FileLoggerService.LogStatic("[Siri] Opened via NSUserActivity shortcut");
            return true;
        }
        return base.ContinueUserActivity(application, userActivity, completionHandler);
    }

    // Called by iOS when Siri needs to handle an INAddTasksIntent.
    // This is the correct entry point for SiriKit intent handling (not ContinueUserActivity).
    // Requires: shortcut donation (done in FinishedLaunching) + user-recorded voice phrase.
    [Export("application:handlerForIntent:")]
    public NSObject GetHandlerForIntent(UIApplication application, INIntent intent)
    {
        if (intent is INAddTasksIntent)
        {
            FileLoggerService.LogStatic("[Siri] GetHandlerForIntent: returning AddTasksIntentHandler");
            return new BrenoMachado.ShopFlow.Platforms.iOS.AddTasksIntentHandler();
        }
        return null!;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
