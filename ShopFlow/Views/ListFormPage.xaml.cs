using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views;

public partial class ListFormPage : ContentPage
{
	public ListFormPage(ListFormViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
