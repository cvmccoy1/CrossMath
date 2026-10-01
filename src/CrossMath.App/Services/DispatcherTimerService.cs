using System.Windows.Threading;

namespace CrossMath.App.Services;

public sealed class DispatcherTimerService : ITimerService
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public DispatcherTimerService()
    {
        _timer.Tick += (_, e) => Tick?.Invoke(this, e);
    }

    public event EventHandler? Tick;

    public bool IsRunning => _timer.IsEnabled;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();
}
