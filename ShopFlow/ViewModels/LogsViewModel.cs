using System.Windows.Input;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public class LogsViewModel : BaseViewModel
    {
        private readonly ILoggerService _loggerService;
        private string _logs;

        public string Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CopyCommand { get; }

        public LogsViewModel(ILoggerService loggerService)
        {
            Title = "App Logs";
            _loggerService = loggerService;

            RefreshCommand = new Command(async () => await LoadLogsAsync());
            ClearCommand = new Command(async () => await ClearLogsAsync());
            CopyCommand = new Command(async () => await CopyLogsAsync());
        }

        public async Task LoadLogsAsync()
        {
            IsBusy = true;
            try
            {
                Logs = await _loggerService.GetLogsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClearLogsAsync()
        {
            await _loggerService.ClearLogsAsync();
            await LoadLogsAsync();
        }

        private async Task CopyLogsAsync()
        {
            if (string.IsNullOrWhiteSpace(Logs)) return;
            await Clipboard.SetTextAsync(Logs);
            await Application.Current.MainPage.DisplayAlert("Success", "Logs copied to clipboard.", "OK");
        }

        public void OnAppearing()
        {
            LoadLogsAsync();
        }
    }
}
