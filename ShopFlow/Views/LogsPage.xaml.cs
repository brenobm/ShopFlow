using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views
{
    public partial class LogsPage : ContentPage
    {
        public LogsPage(LogsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as LogsViewModel)?.OnAppearing();
        }
    }
}
