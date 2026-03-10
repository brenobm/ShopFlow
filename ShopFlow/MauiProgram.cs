using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using BrenoMachado.ShopFlow.Services;
using BrenoMachado.ShopFlow.ViewModels;
using BrenoMachado.ShopFlow.Repositories;
using BrenoMachado.ShopFlow.Views;
using ZXing.Net.Maui.Controls;
using Plugin.LocalNotification;
using Microsoft.Maui.LifecycleEvents;

namespace BrenoMachado.ShopFlow;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Enable AWS SDK Logging
        Amazon.AWSConfigs.LoggingConfig.LogTo = Amazon.LoggingOptions.SystemDiagnostics;
        Amazon.AWSConfigs.LoggingConfig.LogResponses = Amazon.ResponseLoggingOption.Always;
        Amazon.AWSConfigs.LoggingConfig.LogMetrics = true;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Services
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IVoiceService, VoiceService>();
        builder.Services.AddSingleton<ISmartService, SmartService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<IGraphQLService, GraphQLService>();
        builder.Services.AddSingleton<IProductService, OpenFoodFactsService>();
        builder.Services.AddSingleton<ITranslationService, AwsTranslateService>();
        builder.Services.AddSingleton<IAppNotificationService, NotificationService>();
        builder.Services.AddSingleton<IPushService, PushService>();
        builder.Services.AddSingleton<IShoppingRepository, ShoppingRepository>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<ILoggerService, FileLoggerService>();
        builder.Services.AddSingleton<IOfflineSyncService, OfflineSyncService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IBarcodeWorkflowService, BarcodeWorkflowService>();
        builder.Services.AddSingleton<ICrashReporterService, CrashReporterService>();
        
        // Plugin.LocalNotification
        builder.UseLocalNotification();

        // Lifecycle Events (Firebase Init removed to rely on auto-init or resolve build errors)
#if ANDROID
        builder.ConfigureLifecycleEvents(events => {
            events.AddAndroid(android => android.OnCreate((activity, state) => {
                // Plugin.Firebase.CrossFirebase.Initialize(activity);
            }));
        });
#elif IOS
        builder.ConfigureLifecycleEvents(events => {
            events.AddiOS(ios => ios.FinishedLaunching((app, launchOptions) => {
                // Plugin.Firebase.CrossFirebase.Initialize(app, launchOptions);
                return false;
            }));
        });
#endif

        builder.UseBarcodeReader(); // Initialize ZXing

        // ViewModels
        builder.Services.AddSingleton<MainContainerViewModel>();
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddTransient<ActiveListViewModel>();
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ReportsViewModel>();
        builder.Services.AddTransient<LogsViewModel>();

        // Views
        builder.Services.AddSingleton<MainContainerPage>();
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddTransient<ActiveListPage>();
        builder.Services.AddTransient<ItemDetailPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LogsPage>();
        builder.Services.AddTransient<ListFormViewModel>();
        builder.Services.AddTransient<ListFormPage>();

        return builder.Build();
    }
}
