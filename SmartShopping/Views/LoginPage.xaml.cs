using BrenoMachado.SmartShopping.ViewModels;

namespace BrenoMachado.SmartShopping.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
