using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using BrenoMachado.SmartShopping.Services;
using BrenoMachado.SmartShopping.ViewModels;
using BrenoMachado.SmartShopping.Views;

namespace SmartShopping;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
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

        // ViewModels
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddTransient<ActiveListViewModel>();

        // Views
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddTransient<ActiveListPage>();

		return builder.Build();
	}
}
