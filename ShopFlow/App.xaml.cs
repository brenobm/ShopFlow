using Microsoft.Extensions.DependencyInjection;
using BrenoMachado.ShopFlow.Services;

namespace BrenoMachado.ShopFlow;

public partial class App : Application
{
    private readonly IAuthService _authService;

	public App(IAuthService authService, IServiceProvider serviceProvider, ILocalizationService localizationService, IOfflineSyncService offlineSyncService, ICrashReporterService crashReporter)
	{
		InitializeComponent();

        // Global Exception Handling
        AppDomain.CurrentDomain.UnhandledException += async (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            await crashReporter.ReportExceptionAsync(ex, "AppDomain Unhandled");
        };

        TaskScheduler.UnobservedTaskException += async (s, e) =>
        {
            await crashReporter.ReportExceptionAsync(e.Exception, "Unobserved Task");
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