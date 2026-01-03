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

        try
        {
            // Add a small delay for UX
            await Task.Delay(500);

            // Create the auth task
            var authTask = _authService.IsAuthenticatedAsync();
            // Create a timeout task (e.g., 5 seconds)
            var timeoutTask = Task.Delay(5000);

            var completedTask = await Task.WhenAny(authTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout occurred
                System.Diagnostics.Debug.WriteLine($"[LoadingPage] Auth check timed out!");
                await DisplayAlert("Error", "Authentication check timed out. Please try again.", "OK");
                
                // Fallback to login to avoid getting stuck
                var loginPage = _serviceProvider.GetService<LoginPage>();
                Application.Current.MainPage = loginPage;
                return;
            }

            bool isAuthenticated = await authTask; // Result is already available

            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Authenticated: {isAuthenticated}");
            
            // ... strict transition logic ...

    async Task CheckAuthAsync()
    {
        try
        {
            // Add a small delay to ensure UI renders if check is too fast (optional, but good for smooth UX)
            await Task.Delay(500);

            bool isAuthenticated = await _authService.IsAuthenticatedAsync();

            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Authenticated: {isAuthenticated}");

            if (isAuthenticated)
            {
                var pushService = _serviceProvider.GetService<IPushService>();
                Application.Current.MainPage = new AppShell(_authService, pushService);
            }
            else
            {
                 // Resolve LoginPage via DI to ensure ViewModel injection
                 var loginPage = _serviceProvider.GetService<LoginPage>();
                 Application.Current.MainPage = loginPage;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadingPage] Error: {ex}");
            // Optional: Show alert
            await DisplayAlert("Error", $"Startup Error: {ex.Message}", "OK");
        }
    }
}
