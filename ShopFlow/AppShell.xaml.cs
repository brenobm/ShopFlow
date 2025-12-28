using BrenoMachado.ShopFlow.Views;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;
    private readonly IPushService _pushService;

    public AppShell(IAuthService authService, IPushService pushService)
    {
        InitializeComponent();
        _authService = authService;
        _pushService = pushService;
        
        Routing.RegisterRoute(nameof(ActiveListPage), typeof(ActiveListPage));
        Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(ReportsPage), typeof(ReportsPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _pushService.RegisterForPushNotificationsAsync();
    }
}
