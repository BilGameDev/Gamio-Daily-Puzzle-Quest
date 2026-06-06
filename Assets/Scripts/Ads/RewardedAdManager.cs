using Gamio.Core;
using Gamio.Core.Services;
using GoogleMobileAds.Api;
using System;
using UnityEngine;

namespace Gamio.Ads
{
    public class RewardedAdManager : MonoBehaviour, IRewardedAdService
    {
        private RewardedAd _rewardedAd;
        private Action _onRewarded;
        private bool _isLoading;

        private string AdUnitId
        {
            get
            {
                var id = GameSecretsLoader.Load().admobRewardedAdUnitId;
#if UNITY_ANDROID
                return string.IsNullOrEmpty(id) ? "ca-app-pub-5838098451531956/6274858792" : id;
#else
                return "unused";
#endif
            }
        }

        public bool IsAdReady => _rewardedAd != null && _rewardedAd.CanShowAd();

        private static RewardedAdManager _instance;
        public static RewardedAdManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[RewardedAdManager]");
                    _instance = go.AddComponent<RewardedAdManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            GamioAppContext.Register<IRewardedAdService>(this);
        }

        private void Start()
        {
            LoadAd();
        }

        private void OnDestroy()
        {
            GamioAppContext.Unregister<IRewardedAdService>(this);
        }

        public void LoadAd()
        {
            if (_isLoading) return;
            _isLoading = true;

            var adRequest = new AdRequest();
            RewardedAd.Load(AdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                _isLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogError($"[RewardedAdManager] Failed to load: {error?.GetMessage()}");
                    return;
                }
                _rewardedAd = ad;
                RegisterHandlers(ad);
            });
        }

        private void RegisterHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                _rewardedAd = null;
                LoadAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                _rewardedAd = null;
                _onRewarded = null;
                LoadAd();
            };
        }

        public void ShowRewardedAd(Action onRewarded)
        {
            if (!IsAdReady)
            {
                onRewarded?.Invoke();
                return;
            }

            _onRewarded = onRewarded;
            _rewardedAd.Show((Reward reward) =>
            {
                _onRewarded?.Invoke();
                _onRewarded = null;
            });
        }
    }
}
