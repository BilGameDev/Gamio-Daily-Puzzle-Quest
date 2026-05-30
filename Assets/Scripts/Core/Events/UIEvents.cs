using System;
using Google;

public interface IUIEvents : IDisposable
{
    event Action OnTutorialRequested;
    event Action OnSkipTutorialRequested;
    event Action OnBackRequested;
    event Action OnResetRequested;
    event Action OnHintRequested;

    void RequestTutorial();
    void RequestSkipTutorial();
    void RequestBack();
    void RequestReset();
    void RequestHint();
}

public class UIEvents : IUIEvents
{
    public event Action OnTutorialRequested;
    public event Action OnSkipTutorialRequested;
    public event Action OnBackRequested;
    public event Action OnResetRequested;
    public event Action OnHintRequested;

    public void RequestTutorial() => OnTutorialRequested?.Invoke();
    public void RequestSkipTutorial() => OnSkipTutorialRequested?.Invoke();
    public void RequestBack() => OnBackRequested?.Invoke();
    public void RequestReset() => OnResetRequested?.Invoke();
    public void RequestHint() => OnHintRequested?.Invoke();

    public void Dispose()
    {
        OnTutorialRequested = null;
        OnSkipTutorialRequested = null;
        OnBackRequested = null;
        OnResetRequested = null;
        OnHintRequested = null;
    }
}
