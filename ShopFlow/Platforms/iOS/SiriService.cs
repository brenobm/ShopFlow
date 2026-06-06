using Foundation;
using Intents;
using IntentsUI;
using UIKit;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow.Platforms.iOS;

public class SiriService : NSObject, ISiriService, IINUIAddVoiceShortcutViewControllerDelegate, IINUIEditVoiceShortcutViewControllerDelegate
{
    // Build a pre-configured INAddTasksIntent that Siri will associate with ShopFlow.
    private static INAddTasksIntent BuildIntent()
    {
        var intent = new INAddTasksIntent(
            targetTaskList: null,
            taskTitles: null,
            spatialEventTrigger: null,
            temporalEventTrigger: null,
            priority: INTaskPriority.NotFlagged
        );
        intent.SuggestedInvocationPhrase = "Add item to ShopFlow";
        return intent;
    }

    // Called at app launch (after Siri auth granted).
    // Registers the shortcut suggestion so it appears in Settings > Siri & Search > ShopFlow.
    public void DonateAddItemShortcut()
    {
        try
        {
            var intent = BuildIntent();
            var shortcut = new INShortcut(intent);
            INVoiceShortcutCenter.SharedCenter.SetShortcutSuggestions(new[] { shortcut });
            FileLoggerService.LogStatic("[Siri] Shortcut suggestion donated.");
        }
        catch (Exception ex)
        {
            FileLoggerService.LogStatic($"[Siri] DonateAddItemShortcut error: {ex.Message}");
        }
    }

    // Donates an interaction each time the handler actually adds an item.
    // This reinforces Siri's understanding of the app's capabilities.
    public void DonateAddedItemInteraction(string itemName)
    {
        try
        {
            var titles = new[] { new INSpeakableString(itemName) };
            var intent = new INAddTasksIntent(
                targetTaskList: null,
                taskTitles: titles,
                spatialEventTrigger: null,
                temporalEventTrigger: null,
                priority: INTaskPriority.NotFlagged
            );
            var response = new INAddTasksIntentResponse(INAddTasksIntentResponseCode.Success, null);
            var interaction = new INInteraction(intent, response);
            interaction.DonateInteraction(null);
            FileLoggerService.LogStatic($"[Siri] Interaction donated for: {itemName}");
        }
        catch (Exception ex)
        {
            FileLoggerService.LogStatic($"[Siri] DonateAddedItemInteraction error: {ex.Message}");
        }
    }

    // Presents the system UI to let the user record a Siri voice phrase for the shortcut.
    // If the user already has a phrase set up, shows the edit UI instead.
    public async Task PresentAddVoiceShortcutAsync()
    {
        try
        {
            var rootVC = GetTopViewController();
            if (rootVC == null)
            {
                FileLoggerService.LogStatic("[Siri] PresentAddVoiceShortcut: no root VC found.");
                return;
            }

            var intent = BuildIntent();
            var shortcut = new INShortcut(intent);

            // Check if the user already has a voice shortcut for this intent.
            var existingShortcuts = await GetExistingVoiceShortcutsAsync();
            var existing = existingShortcuts?.FirstOrDefault(vs =>
                vs.Shortcut?.Intent is INAddTasksIntent);

            if (existing != null)
            {
                // Show edit UI.
                var editVC = new INUIEditVoiceShortcutViewController(existing);
                editVC.Delegate = this;
                rootVC.PresentViewController(editVC, animated: true, completionHandler: null);
            }
            else
            {
                // Show add UI.
                var addVC = new INUIAddVoiceShortcutViewController(shortcut);
                addVC.Delegate = this;
                rootVC.PresentViewController(addVC, animated: true, completionHandler: null);
            }
        }
        catch (Exception ex)
        {
            FileLoggerService.LogStatic($"[Siri] PresentAddVoiceShortcut error: {ex.Message}");
        }
    }

    // IINUIAddVoiceShortcutViewControllerDelegate
    public void DidFinish(INUIAddVoiceShortcutViewController controller, INVoiceShortcut voiceShortcut, NSError error)
    {
        controller.DismissViewController(animated: true, completionHandler: null);
        if (error != null)
            FileLoggerService.LogStatic($"[Siri] Add shortcut error: {error.LocalizedDescription}");
        else
            FileLoggerService.LogStatic("[Siri] Voice shortcut added successfully.");
    }

    public void DidCancel(INUIAddVoiceShortcutViewController controller)
    {
        controller.DismissViewController(animated: true, completionHandler: null);
        FileLoggerService.LogStatic("[Siri] Add shortcut cancelled.");
    }

    // IINUIEditVoiceShortcutViewControllerDelegate
    public void DidUpdate(INUIEditVoiceShortcutViewController controller, INVoiceShortcut voiceShortcut, NSError error)
    {
        controller.DismissViewController(animated: true, completionHandler: null);
        if (error != null)
            FileLoggerService.LogStatic($"[Siri] Edit shortcut error: {error.LocalizedDescription}");
        else
            FileLoggerService.LogStatic("[Siri] Voice shortcut updated.");
    }

    public void DidDelete(INUIEditVoiceShortcutViewController controller, NSObject deletedVoiceShortcutIdentifier)
    {
        controller.DismissViewController(animated: true, completionHandler: null);
        FileLoggerService.LogStatic("[Siri] Voice shortcut deleted.");
    }

    public void DidCancel(INUIEditVoiceShortcutViewController controller)
    {
        controller.DismissViewController(animated: true, completionHandler: null);
        FileLoggerService.LogStatic("[Siri] Edit shortcut cancelled.");
    }

    private static Task<INVoiceShortcut[]?> GetExistingVoiceShortcutsAsync()
    {
        var tcs = new TaskCompletionSource<INVoiceShortcut[]?>();
        INVoiceShortcutCenter.SharedCenter.GetAllVoiceShortcuts((shortcuts, error) =>
        {
            if (error != null)
            {
                FileLoggerService.LogStatic($"[Siri] GetAllVoiceShortcuts error: {error.LocalizedDescription}");
                tcs.SetResult(null);
            }
            else
            {
                tcs.SetResult(shortcuts);
            }
        });
        return tcs.Task;
    }

    private static UIViewController? GetTopViewController()
    {
        var windowScene = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .FirstOrDefault();
        var window = windowScene?.Windows.FirstOrDefault(w => w.IsKeyWindow)
                     ?? windowScene?.Windows.FirstOrDefault();
        var rootVC = window?.RootViewController;
        return GetTopViewControllerFrom(rootVC);
    }

    private static UIViewController? GetTopViewControllerFrom(UIViewController? vc)
    {
        if (vc == null) return null;
        if (vc.PresentedViewController != null)
            return GetTopViewControllerFrom(vc.PresentedViewController);
        if (vc is UINavigationController nav)
            return GetTopViewControllerFrom(nav.VisibleViewController);
        if (vc is UITabBarController tab)
            return GetTopViewControllerFrom(tab.SelectedViewController);
        return vc;
    }
}
