using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow.Views;

public partial class LoadingPage : ContentPage
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    public LoadingPage(IAuthService authService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _authService = authService;
        _serviceProvider = serviceProvider;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAuthAsync();
    }

    async Task CheckAuthAsync()
    {
        // Add a small delay to ensure UI renders if check is too fast (optional, but good for smooth UX)
        await Task.Delay(500);

        bool isAuthenticated = await _authService.IsAuthenticatedAsync();

        if (isAuthenticated)
        {
            Application.Current.MainPage = new AppShell(_authService);
        }
        else
        {
             // Resolve LoginPage via DI to ensure ViewModel injection
             var loginPage = _serviceProvider.GetService<LoginPage>();
             Application.Current.MainPage = loginPage;
        }
    }
}
