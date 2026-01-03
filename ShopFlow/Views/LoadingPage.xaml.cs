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
        try
        {
            // Logging start
            FileLoggerService.LogStatic("[LoadingPage] Starting CheckAuthAsync...");
            
            // Add a small delay for UX
            await Task.Delay(500);

            // Create the auth task
            var authTask = _authService.IsAuthenticatedAsync();
            // Create a timeout task (e.g., 5 seconds)
            var timeoutTask = Task.Delay(5000);

            var completedTask = await Task.WhenAny(authTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                FileLoggerService.LogStatic("[LoadingPage] Auth check timed out!");
                await DisplayAlert("Error", "Authentication check timed out. Please try again.", "OK");
                // REVERT: Uncomment to enable auto-navigation
                // var loginPage = _serviceProvider.GetService<LoginPage>();
                // Application.Current.MainPage = loginPage;
                return;
            }

            bool isAuthenticated = await authTask; 
            FileLoggerService.LogStatic($"[LoadingPage] Authenticated: {isAuthenticated}");

            if (isAuthenticated)
            {
                FileLoggerService.LogStatic("[LoadingPage] Preparing to navigate to AppShell (Authenticated)...");
                // REVERT: Uncomment to enable auto-navigation
                // var pushService = _serviceProvider.GetService<IPushService>();
                // Application.Current.MainPage = new AppShell(_authService, pushService);
                
                await DisplayAlert("Debug", "Authenticated! Ready to navigate to AppShell. Check Logs.", "OK");
            }
            else
            {
                 FileLoggerService.LogStatic("[LoadingPage] Not Authenticated. Preparing to navigate to LoginPage...");
                 // REVERT: Uncomment to enable auto-navigation
                 // var loginPage = _serviceProvider.GetService<LoginPage>();
                 // Application.Current.MainPage = loginPage;
                 
                 await DisplayAlert("Debug", "Not Authenticated. Ready to navigate to LoginPage. Check Logs.", "OK");
            }
        }
        catch (Exception ex)
        {
            FileLoggerService.LogStatic($"[LoadingPage] Error: {ex}");
            await DisplayAlert("Error", $"Startup Error: {ex.Message}", "OK");
            
            // REVERT: Uncomment to enable auto-navigation
            // var loginPage = _serviceProvider.GetService<LoginPage>();
            // Application.Current.MainPage = loginPage;
        }
    }
    
    // DEBUGGING HANDLERS
    private async void OnShowLogsClicked(object sender, EventArgs e)
    {
        try 
        {
            var logger = FileLoggerService.Instance;
            var vm = new BrenoMachado.ShopFlow.ViewModels.LogsViewModel(logger);
            var page = new LogsPage(vm);
            await Navigation.PushModalAsync(page);
        }
        catch(Exception ex)
        {
            await DisplayAlert("Error", "Could not open logs: " + ex.Message, "OK");
        }
    }

    private void OnManualLoginClicked(object sender, EventArgs e)
    {
        try
        {
             var loginPage = _serviceProvider.GetService<LoginPage>();
             Application.Current.MainPage = loginPage;
        }
        catch(Exception ex)
        {
            DisplayAlert("Error", "Could not navigate: " + ex.Message, "OK");
        }
    }
}
