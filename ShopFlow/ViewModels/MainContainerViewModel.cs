using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace BrenoMachado.ShopFlow.ViewModels
{
    public partial class MainContainerViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool _isListsViewVisible = true;

        [ObservableProperty]
        private bool _isReportsViewVisible = false;

        public MainContainerViewModel()
        {
            try
            {
                FileLoggerService.LogStatic("[MainContainerViewModel] Constructor started.");
                Title = "ShopFlow";
                IsListsViewVisible = true;
                FileLoggerService.LogStatic("[MainContainerViewModel] Constructor finished.");
            }
            catch (Exception ex)
            {
                FileLoggerService.LogStatic($"[MainContainerViewModel] CRASH in Constructor: {ex}");
            }
        }

        [RelayCommand]
        private void SwitchToLists()
        {
            IsListsViewVisible = true;
            IsReportsViewVisible = false;
            Title = "ShopFlow";
        }

        [RelayCommand]
        private void SwitchToReports()
        {
            IsListsViewVisible = false;
            IsReportsViewVisible = true;
            Title = "Reports & History";
        }
    }
}
