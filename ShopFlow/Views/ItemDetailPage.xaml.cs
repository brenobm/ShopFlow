using BrenoMachado.ShopFlow.ViewModels;

namespace BrenoMachado.ShopFlow.Views;

public partial class ItemDetailPage : ContentPage
{
	public ItemDetailPage(ItemDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
