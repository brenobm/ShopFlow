using BrenoMachado.ShopFlow.ViewModels;
using Microsoft.Maui.Controls;

namespace BrenoMachado.ShopFlow.Views.Partials
{
    public partial class DashboardView : ContentView
    {
        public DashboardView()
        {
            InitializeComponent();
            try
            {
                if (IPlatformApplication.Current?.Services != null)
                {
                     BindingContext = IPlatformApplication.Current.Services.GetService<DashboardViewModel>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardView] Error resolving VM: {ex}");
            }
        }
    }
}
