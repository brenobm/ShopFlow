namespace BrenoMachado.ShopFlow.Services;

public static class ServiceHelper
{
    public static IServiceProvider Services { get; private set; }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
    }

    public static TService GetService<TService>() => Services.GetService<TService>();
}
