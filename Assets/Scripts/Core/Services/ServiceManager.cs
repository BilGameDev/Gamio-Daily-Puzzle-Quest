using Gamio.Core.Services;
using Gamio.Ads;
using UnityEngine;

namespace Gamio.Core
{
    [DefaultExecutionOrder(-100)]
    public class ServiceManager : MonoBehaviour
    {
        CloudAPIService cloudAPIService;
        ILoginEvents loginEvents;

        private void Awake()
        {
            GamioAppContext.Register<ILoginEvents>(loginEvents = new LoginEvents());
            GamioAppContext.Register<IUIEvents>(new UIEvents());
            GamioAppContext.Register<ICloudDataEvents>(new CloudDataEvents());
            GamioAppContext.Register(cloudAPIService = new CloudAPIService());
            GamioAppContext.Register(new AuthService(cloudAPIService, loginEvents));
            GamioAppContext.Register(new OfflineQueue(cloudAPIService));
            GamioAppContext.Register(new ConnectivityService());
            GamioAppContext.Register<IRewardedAdService>(RewardedAdManager.Instance);
        }
    }
}
