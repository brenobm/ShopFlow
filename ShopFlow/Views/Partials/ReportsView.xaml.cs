using BrenoMachado.ShopFlow.ViewModels;
using Microsoft.Maui.Controls;

namespace BrenoMachado.ShopFlow.Views.Partials
{
    public partial class ReportsView : ContentView
    {
        public ReportsView()
        {
            InitializeComponent();
             if (IPlatformApplication.Current?.Services != null)
            {
                 BindingContext = IPlatformApplication.Current.Services.GetService<ReportsViewModel>();
            }
        }
    }
}
