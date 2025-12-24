using Microsoft.Extensions.DependencyInjection;
using BrenoMachado.SmartShopping.Services;

namespace SmartShopping;

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