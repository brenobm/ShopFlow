using System.Windows.Input;
using BrenoMachado.SmartShopping.Services;

namespace BrenoMachado.SmartShopping.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;

        public ICommand ClearDataCommand { get; }

        public string Version => "1.0.0";

        public SettingsViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Settings";
            ClearDataCommand = new Command(async () => await ClearDataAsync());
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
