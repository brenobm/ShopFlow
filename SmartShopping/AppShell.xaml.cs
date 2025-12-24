using BrenoMachado.SmartShopping.Views;
using BrenoMachado.SmartShopping.Services;

namespace SmartShopping;

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
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await _authService.IsAuthenticatedAsync())
        {
            await Current.GoToAsync($"//{nameof(LoginPage)}");
        }
    }
}
