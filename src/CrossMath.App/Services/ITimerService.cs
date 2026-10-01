namespace CrossMath.App.Services;

/// <summary>A one-second tick source, abstracted so view models can be tested without a dispatcher.</summary>
public interface ITimerService
{
    event EventHandler? Tick;
    bool IsRunning { get; }
    void Start();
    void Stop();
}
