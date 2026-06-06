using Gamio.Core;
using Gamio.Core.Services;
using GoogleMobileAds.Api;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamio.Ads
{
    public class RewardedAdManager : MonoBehaviour, IRewardedAdService
    {
        private RewardedAd _rewardedAd;
        private Action _onRewarded;
        private bool _isLoading;

        private const string TestAdUnitId = "ca-app-pub-3940256099942544/5224354917";

        private string AdUnitId
        {
            get
            {
                if (Debug.isDebugBuild)
                    return TestAdUnitId;
                var id = GameSecretsLoader.Load().admobRewardedAdUnitId;
#if UNITY_ANDROID
                return string.IsNullOrEmpty(id) ? TestAdUnitId : id;
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
                    _instance = FindFirstObjectByType<RewardedAdManager>();
                }
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
            if (_instance == null)
                _instance = this;
        }

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TestDeviceIds = new List<string>
                {
                    AdRequest.TestDeviceSimulator,
#if UNITY_ANDROID
                    "2422B7F946C5CEE98E81584DEE454F1F"
#endif
                }
            });
#endif

            if (GMASDK.IsInitialized)
            {
                LoadAd();
            }
            else
            {
                GMASDK.OnInitialized += LoadAd;
            }
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
            ad.OnAdFullScreenContentOpened += () =>
            {
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                CleanupAd();
                LoadAd();
            };
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                CleanupAd();
                _onRewarded = null;
                LoadAd();
            };
        }

        private void CleanupAd()
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
        }

        public void ShowRewardedAd(Action onRewarded)
        {
            if (!IsAdReady)
            {
                Debug.Log("[RewardedAdManager] Ad not ready, proceeding without ad");
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
