using BrenoMachado.ShopFlow.Views;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        
        Routing.RegisterRoute(nameof(ActiveListPage), typeof(ActiveListPage));
        Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));

    }


}
