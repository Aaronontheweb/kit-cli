namespace KitCLI.Helpers;

public interface IProgressIndicator : IDisposable
{
    void Report(string message);
    void Report(int current, int total);
    void Complete(string? message = null);
}

public sealed class ProgressIndicator : IProgressIndicator
{
    private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _animationTask;
    private readonly string _title;
    private int _spinnerIndex;
    private string _currentMessage = string.Empty;
    private bool _isCompleted;
    
    public ProgressIndicator(string title)
    {
        _title = title;
        
        if (!Console.IsOutputRedirected)
        {
            Console.Write($"{title}... ");
            _animationTask = Task.Run(AnimateAsync);
        }
        else
        {
            _animationTask = Task.CompletedTask;
        }
    }
    
    private async Task AnimateAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            if (!Console.IsOutputRedirected)
            {
                var spinner = SpinnerFrames[_spinnerIndex++ % SpinnerFrames.Length];
                Console.Write($"\r{_title}... {spinner} {_currentMessage}");
            }
            
            try
            {
                await Task.Delay(100, _cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
    
    public void Report(string message)
    {
        _currentMessage = message;
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine($"{_title}: {message}");
        }
    }
    
    public void Report(int current, int total)
    {
        var percentage = total > 0 ? (current * 100.0 / total) : 0;
        _currentMessage = $"{current:N0}/{total:N0} ({percentage:F0}%)";
        
        if (Console.IsOutputRedirected)
        {
            if (current % 100 == 0 || current == total)
            {
                Console.WriteLine($"{_title}: {_currentMessage}");
            }
        }
    }
    
    public void Complete(string? message = null)
    {
        if (_isCompleted) return;
        _isCompleted = true;
        
        _cts.Cancel();
        
        try
        {
            _animationTask.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch { }
        
        if (!Console.IsOutputRedirected)
        {
            Console.Write($"\r{_title}... ✓ {message ?? "Done"}".PadRight(Console.WindowWidth - 1));
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"{_title}: {message ?? "Done"}");
        }
    }
    
    public void Dispose()
    {
        if (!_isCompleted)
        {
            Complete();
        }
        _cts.Dispose();
    }
}

public sealed class SilentProgressIndicator : IProgressIndicator
{
    public void Report(string message) { }
    public void Report(int current, int total) { }
    public void Complete(string? message = null) { }
    public void Dispose() { }
}

public static class ProgressIndicatorFactory
{
    public static IProgressIndicator Create(string title, bool enabled = true)
    {
        if (!enabled || Console.IsOutputRedirected)
            return new SilentProgressIndicator();
        
        return new ProgressIndicator(title);
    }
}