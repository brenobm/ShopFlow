using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as DashboardViewModel)?.OnAppearing();
        }
    }
}
