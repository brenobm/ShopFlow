using BrenoMachado.SmartShopping.ViewModels;

namespace BrenoMachado.SmartShopping.Views
{
    public partial class ActiveListPage : ContentPage
    {
        public ActiveListPage(ActiveListViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
