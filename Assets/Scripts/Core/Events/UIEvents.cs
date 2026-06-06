using System;
using Gamio.Core;
using Google;

public interface IUIEvents : IDisposable
{
    event Action OnTutorialRequested;
    event Action OnSkipTutorialRequested;
    event Action OnBackRequested;
    event Action OnResetRequested;
    event Action OnHintRequested;
    event Action OnChallengeRequested;
    event Action<float> OnChallengeSolved;
    event Action<string> OnGameSceneRequested;
    event Action<IGame> OnGameLaunched;
    event Action<Action, Action> OnAdGateRequested;
    event Action OnPrivacyRequested;
    event Action<string, string> OnNotificationRequested;

    void RequestTutorial();
    void RequestSkipTutorial();
    void RequestBack();
    void RequestReset();
    void RequestHint();
    void RequestChallenge();
    void SolvedChallenge(float solveTime);
    void RequestGameScene(string gameScene);
    void LaunchGame(IGame game);
    void RequestAdGate(Action onProceed, Action onCancel);
    void RequestPrivacy();
    void RequestNotification(string title, string message);
}

public class UIEvents : IUIEvents
{
    public event Action OnTutorialRequested;
    public event Action OnSkipTutorialRequested;
    public event Action OnBackRequested;
    public event Action OnResetRequested;
    public event Action OnHintRequested;
    public event Action OnChallengeRequested;
    public event Action<float> OnChallengeSolved;
    public event Action<string> OnGameSceneRequested;
    public event Action<IGame> OnGameLaunched;
    public event Action<Action, Action> OnAdGateRequested;
    public event Action OnPrivacyRequested;
    public event Action<string, string> OnNotificationRequested;

    public void RequestTutorial() => OnTutorialRequested?.Invoke();
    public void RequestSkipTutorial() => OnSkipTutorialRequested?.Invoke();
    public void RequestBack() => OnBackRequested?.Invoke();
    public void RequestReset() => OnResetRequested?.Invoke();
    public void RequestHint() => OnHintRequested?.Invoke();
    public void RequestChallenge() => OnChallengeRequested?.Invoke();
    public void SolvedChallenge(float solveTime) => OnChallengeSolved?.Invoke(solveTime);
    public void RequestGameScene(string gameScene) => OnGameSceneRequested?.Invoke(gameScene);
    public void LaunchGame(IGame game) => OnGameLaunched?.Invoke(game);
    public void RequestAdGate(Action onProceed, Action onCancel) => OnAdGateRequested?.Invoke(onProceed, onCancel);
    public void RequestPrivacy() => OnPrivacyRequested?.Invoke();
    public void RequestNotification(string title, string message) => OnNotificationRequested?.Invoke(title, message);

    public void Dispose()
    {
        OnTutorialRequested = null;
        OnSkipTutorialRequested = null;
        OnBackRequested = null;
        OnResetRequested = null;
        OnHintRequested = null;
        OnChallengeRequested = null;
        OnGameSceneRequested = null;
        OnPrivacyRequested = null;
        OnNotificationRequested = null;
    }
}
