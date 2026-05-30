using Gamio.Core.Services;
using UnityEngine;

namespace Gamio.Core
{
    [DefaultExecutionOrder(-100)] // Make sure it runs before other managers
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
            GamioAppContext.Register<IGameSeedProvider>(new DefaultSeedProvider());
            GamioAppContext.Register(new ConnectivityService());
            GamioAppContext.Register(new TutorialService());
        }
    }
}
