using System.Windows.Input;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAppNotificationService _notificationService;

        public ICommand ClearDataCommand { get; }
        public ICommand TestPushCommand { get; }

        public string Version => "1.0.0";

        public SettingsViewModel(IDatabaseService databaseService, IAppNotificationService notificationService)
        {
            _databaseService = databaseService;
            _notificationService = notificationService;
            Title = "Settings";
            ClearDataCommand = new Command(async () => await ClearDataAsync());
            TestPushCommand = new Command(async () => await TestPushAsync());
            
            // Auto-init on settings open (or app start ideally)
            _ = _notificationService.InitializeAsync();
        }

        async Task TestPushAsync()
        {
            await _notificationService.ShowLocalNotification("Test Notification", "This is a local alert from ShopFlow!");
            // Also try to get token
            try {
                var token = await _notificationService.GetDeviceTokenAsync();
                Console.WriteLine($"Token: {token}");
            } catch {}
        }

        async Task ClearDataAsync()
        {
            bool confirm = await Shell.Current.DisplayAlert("Warning", "This will delete ALL your lists and items. This action cannot be undone.", "Delete All", "Cancel");
            if (confirm)
            {
                // We need a method to clear everything. 
                // Currently IDatabaseService doesn't have a "Nuke" option.
                // We can iterate or drop tables. Drop tables is faster.
                // But we need to expose it.
                // For now let's just delete lists which cascades?
                // Our DeleteListAsync cascades items.
                
                var lists = await _databaseService.GetListsAsync();
                foreach(var list in lists)
                {
                    await _databaseService.DeleteListAsync(list);
                }

                await Shell.Current.DisplayAlert("Success", "All data cleared.", "OK");
            }
        }
    }
}
