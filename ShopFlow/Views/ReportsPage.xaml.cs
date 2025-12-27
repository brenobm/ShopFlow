using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views;

public partial class ReportsPage : ContentPage
{
	private readonly ReportsViewModel _viewModel;

	public ReportsPage(ReportsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadReportsAsync();
    }
}
