using BrenoMachado.ShopFlow.ViewModels;
using Microsoft.Maui.Controls;

namespace BrenoMachado.ShopFlow.Views.Partials
{
    public partial class ReportsView : ContentView
    {
        public ReportsView()
        {
            InitializeComponent();
            try
            {
                 if (IPlatformApplication.Current?.Services != null)
                {
                     BindingContext = IPlatformApplication.Current.Services.GetService<ReportsViewModel>();
                }
            }
             catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReportsView] Error resolving VM: {ex}");
            }
        }
    }
}
