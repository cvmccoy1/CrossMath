using System.Windows;
using CrossMath.App.Services;
using CrossMath.App.ViewModels;
using CrossMath.App.Views;
using CrossMath.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrossMath.App;

/// <summary>Composition root: wires services, view models and views together.</summary>
public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = new ServiceCollection()
            .AddSingleton<IPuzzleGenerator>(_ => new PuzzleGenerator())
            .AddSingleton<ITimerService, DispatcherTimerService>()
            .AddSingleton<ISettingsService>(_ => new JsonSettingsService(JsonSettingsService.DefaultPath))
            .AddSingleton<MainViewModel>()
            .AddSingleton<MainWindow>()
            .BuildServiceProvider();

        var viewModel = _services.GetRequiredService<MainViewModel>();
        var window = _services.GetRequiredService<MainWindow>();
        window.DataContext = viewModel;
        window.Show();

        viewModel.NewGameCommand.Execute(null);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}
