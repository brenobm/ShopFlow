using BrenoMachado.SmartShopping.ViewModels;

namespace BrenoMachado.SmartShopping.Views
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
