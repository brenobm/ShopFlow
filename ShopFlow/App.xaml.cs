using Microsoft.Extensions.DependencyInjection;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow;

public partial class App : Application
{
    private readonly IAuthService _authService;

	public App(IAuthService authService, IServiceProvider serviceProvider, ILocalizationService localizationService, IOfflineSyncService offlineSyncService)
	{
		InitializeComponent();

        // Global Exception Handling
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            FileLoggerService.LogStatic($"[CRITICAL] Unhandled Exception: {ex?.Message} | {ex?.StackTrace}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            FileLoggerService.LogStatic($"[CRITICAL] Unobserved Task Exception: {e.Exception?.Message} | {e.Exception?.StackTrace}");
            e.SetObserved();
        };

        _authService = authService;
        _serviceProvider = serviceProvider;
        ServiceHelper.Initialize(serviceProvider);
        
        // Ensure culture is set immediately
        localizationService.Initialize();
        
        // Weekly Sync
        Task.Run(async () => await offlineSyncService.SyncOpenFoodFactsAsync());
	}

    private readonly IServiceProvider _serviceProvider;

	protected override Window CreateWindow(IActivationState? activationState)
	{
        // Start with LoadingPage to handle async Auth check safely
        return new Window(new Views.LoadingPage(_authService, _serviceProvider));
	}
}