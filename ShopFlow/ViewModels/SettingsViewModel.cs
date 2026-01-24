using System.Windows.Input;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Views;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAppNotificationService _notificationService;

        public ICommand ClearDataCommand { get; }
        public ICommand TestPushCommand { get; }

        public string Version => "1.0.0";

        public class LanguageModel
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string Flag { get; set; }
            public string DisplayName => $"{Flag} {Name}";
        }

        public List<LanguageModel> Languages { get; } = new List<LanguageModel>
        {
            new LanguageModel { Name = "English - Canada", Code = "en-CA", Flag = "🇨🇦" },
            new LanguageModel { Name = "Português - Brasil", Code = "pt-BR", Flag = "🇧🇷" }
        };

        private LanguageModel _selectedLanguage;
        public LanguageModel SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();
                    ChangeLanguage(value);
                }
            }
        }

        private readonly ILocalizationService _localizationService;

        public ICommand GoToLogsCommand { get; }

        // ...

        public SettingsViewModel(IDatabaseService databaseService, IAppNotificationService notificationService, ILocalizationService localizationService)
        {
            _databaseService = databaseService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            
            Title = Resources.Strings.AppResources.Settings;
            
            // Set initial selected
            var currentCode = _localizationService.GetCurrentCulture().Name;
            // Default to EN if not found or partial match
            SelectedLanguage = Languages.FirstOrDefault(l => l.Code == currentCode) 
                             ?? Languages.FirstOrDefault(l => currentCode.StartsWith("pt") && l.Code == "pt-BR")
                             ?? Languages.FirstOrDefault(l => l.Code == "en-CA");

            ClearDataCommand = new Command(async () => await ClearDataAsync());
            TestPushCommand = new Command(async () => await TestPushAsync());
            GoToLogsCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LogsPage)));
            // Auto-init on settings open (or app start ideally)
            _ = _notificationService.InitializeAsync();
        }

        private async void ChangeLanguage(LanguageModel language)
        {
            if (language == null) return;
            
            var current = _localizationService.GetCurrentCulture().Name;
            if (current != language.Code)
            {
                _localizationService.SetCulture(language.Code);
                await Shell.Current.DisplayAlert(Resources.Strings.AppResources.RestartRequired, Resources.Strings.AppResources.RestartMessage, "OK");
            }
        }


        async Task TestPushAsync()
        {
            await _notificationService.ShowLocalNotification(Resources.Strings.AppResources.Notifications, Resources.Strings.AppResources.TestNotification);
            // Also try to get token
            try {
                var token = await _notificationService.GetDeviceTokenAsync();
                FileLoggerService.LogStatic($"Token: {token}");
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
