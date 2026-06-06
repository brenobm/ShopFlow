using System.Windows.Input;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.Repositories;
using BrenoMachado.ShopFlow.Views;
using Microsoft.Maui.ApplicationModel;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IShoppingRepository _shoppingRepository;
        private readonly IAppNotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly ISiriService _siriService;

        public ICommand ClearDataCommand { get; }
        public ICommand TestPushCommand { get; }
        public ICommand SetupSiriCommand { get; }

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

        public SettingsViewModel(IShoppingRepository shoppingRepository, IAppNotificationService notificationService, ILocalizationService localizationService, IDialogService dialogService, ISiriService siriService)
        {
            _shoppingRepository = shoppingRepository;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _dialogService = dialogService;
            _siriService = siriService;
            
            Title = Resources.Strings.AppResources.Settings;
            
            // Set initial selected
            var currentCode = _localizationService.GetCurrentCulture().Name;
            // Default to EN if not found or partial match
            SelectedLanguage = Languages.FirstOrDefault(l => l.Code == currentCode) 
                             ?? Languages.FirstOrDefault(l => currentCode.StartsWith("pt") && l.Code == "pt-BR")
                             ?? Languages.FirstOrDefault(l => l.Code == "en-CA");

            ClearDataCommand = new Command(async () => await ClearDataAsync());
            TestPushCommand = new Command(async () => await TestPushAsync());
            SetupSiriCommand = new Command(async () => await _siriService.PresentAddVoiceShortcutAsync());
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
                await _dialogService.DisplayAlertAsync(Resources.Strings.AppResources.RestartRequired, Resources.Strings.AppResources.RestartMessage, "OK");
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
            bool confirm = await _dialogService.DisplayAlertAsync("Warning", "This will delete ALL your lists and items. This action cannot be undone.", "Delete All", "Cancel");
            if (confirm)
            {
                // We need a method to clear everything. 
                // Currently IDatabaseService doesn't have a "Nuke" option.
                // We can iterate or drop tables. Drop tables is faster.
                // But we need to expose it.
                // For now let's just delete lists which cascades?
                // Our DeleteListAsync cascades items.
                
                var lists = await _shoppingRepository.GetListsAsync();
                foreach(var list in lists)
                {
                    await _shoppingRepository.DeleteListAsync(list);
                }

                await _dialogService.DisplayAlertAsync("Success", "All data cleared.", "OK");
            }
        }
    }
}
