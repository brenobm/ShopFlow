using BrenoMachado.SmartShopping.ViewModels;

namespace BrenoMachado.SmartShopping.Views;

public partial class ItemDetailPage : ContentPage
{
	public ItemDetailPage(ItemDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
