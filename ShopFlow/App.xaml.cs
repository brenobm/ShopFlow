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
		return new Window(new AppShell(_authService));
	}
}