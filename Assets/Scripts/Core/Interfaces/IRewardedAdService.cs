using System;

namespace Gamio.Core
{
    public interface IRewardedAdService
    {
        bool IsAdReady { get; }
        void ShowRewardedAd(Action onRewarded);
    }
}
