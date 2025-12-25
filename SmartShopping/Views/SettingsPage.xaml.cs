using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
