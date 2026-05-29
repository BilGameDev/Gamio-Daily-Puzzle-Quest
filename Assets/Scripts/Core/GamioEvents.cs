using System;

namespace Gamio.Core
{
    public static class GamioEvents
    {
        public static IGame currentGame;
        public static event Action OnTutorialRequested;
        public static event Action OnSkipTutorialRequested;
        public static event Action OnBackRequested;
        public static event Action OnResetRequested;
        public static event Action OnHintRequested;
        public static event Action OnLoginRequested;
        public static event Action OnSilentLoginRequested;
        public static event Action OnLogoutRequested;

        public static void RequestTutorial() => OnTutorialRequested?.Invoke();
        public static void RequestSkipTutorial() => OnSkipTutorialRequested?.Invoke();
        public static void RequestBack() => OnBackRequested?.Invoke();
        public static void RequestReset() => OnResetRequested?.Invoke();
        public static void RequestHint() => OnHintRequested?.Invoke();
        public static void RequestLogin() => OnLoginRequested?.Invoke();
        public static void RequestSilentLogin() => OnSilentLoginRequested?.Invoke();
        public static void RequestLogout() => OnLogoutRequested?.Invoke();

        /// <summary>
        /// Set to true while the daily challenge is in progress.
        /// Used by TopBarController to show challenge-specific back confirmation text.
        /// </summary>
        public static bool IsChallengeActive { get; set; }
    }
}