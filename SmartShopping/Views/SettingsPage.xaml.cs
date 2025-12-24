using BrenoMachado.SmartShopping.ViewModels;

namespace BrenoMachado.SmartShopping.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
