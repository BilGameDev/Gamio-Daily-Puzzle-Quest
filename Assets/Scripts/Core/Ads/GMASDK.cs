using System;

namespace Gamio.Core
{
    public static class GMASDK
    {
        public static bool IsInitialized { get; private set; }
        public static event Action OnInitialized;

        public static void NotifyInitialized()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            OnInitialized?.Invoke();
            OnInitialized = null;
        }
    }
}
