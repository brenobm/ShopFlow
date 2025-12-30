using Microsoft.Extensions.DependencyInjection;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow;

public partial class App : Application
{
    private readonly IAuthService _authService;

	public App(IAuthService authService, IServiceProvider serviceProvider, ILocalizationService localizationService)
	{
		InitializeComponent();
        _authService = authService;
        _serviceProvider = serviceProvider;
        
        // Ensure culture is set immediately
        localizationService.Initialize();
	}

    private readonly IServiceProvider _serviceProvider;

	protected override Window CreateWindow(IActivationState? activationState)
	{
        // Start with LoadingPage to handle async Auth check safely
        return new Window(new Views.LoadingPage(_authService, _serviceProvider));
	}
}