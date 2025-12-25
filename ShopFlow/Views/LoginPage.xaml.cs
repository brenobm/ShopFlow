using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
