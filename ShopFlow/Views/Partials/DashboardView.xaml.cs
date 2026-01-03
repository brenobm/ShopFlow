using BrenoMachado.ShopFlow.ViewModels;
using Microsoft.Maui.Controls;

namespace BrenoMachado.ShopFlow.Views.Partials
{
    public partial class DashboardView : ContentView
    {
        public DashboardView()
        {
            InitializeComponent();
            // Resolve ViewModel. ideally passed in via Constructor if instantiated by DI, but for XAML inclusion we might need manual resolve or BindingContext inheritance
            // Since this is a sub-view, let's grab the VM from the ServiceProvider
            if (IPlatformApplication.Current?.Services != null)
            {
                 BindingContext = IPlatformApplication.Current.Services.GetService<DashboardViewModel>();
            }
        }
    }
}
