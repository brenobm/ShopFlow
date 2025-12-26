using Microsoft.Extensions.DependencyInjection;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow;

public partial class App : Application
{
    private readonly IAuthService _authService;

	public App(IAuthService authService)
	{
		InitializeComponent();
        _authService = authService;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
        // Check if user is authenticated
        // Note: This is a synchronous check for startup. The AuthService might need a sync way or we run async over sync carefully.
        // Better pattern: Start with a LoadingPage or determine state. 
        // For now, let's assume we can check or default to Login.
        
        bool isAuthenticated = Task.Run(async () => await _authService.IsAuthenticatedAsync()).Result;

        if (isAuthenticated)
        {
		    return new Window(new AppShell(_authService));
        }
        else
        {
            // We need to resolve LoginPage from DI or create it.
            // Since we are in App.xaml.cs, we might need to access IServiceProvider to get LoginPage if it has dependencies (ViewModel).
            // However, typical Maui template registers pages.
            
            // To get the LoginPage from DI:
            var loginPage = Handler?.MauiContext?.Services.GetService<Views.LoginPage>() 
                         ?? new Views.LoginPage(Handler?.MauiContext?.Services.GetService<ViewModels.LoginViewModel>() 
                                                ?? new ViewModels.LoginViewModel(_authService));
            
            return new Window(loginPage);
        }
	}
}