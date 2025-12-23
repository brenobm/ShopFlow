using BrenoMachado.SmartShopping.Views;

namespace SmartShopping;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(ActiveListPage), typeof(ActiveListPage));
	}
}
