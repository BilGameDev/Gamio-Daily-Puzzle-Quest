using Gamio.Core.Services;
using UnityEngine;

namespace Gamio.Core
{
    [DefaultExecutionOrder(-100)] // Make sure it runs before other managers
    public class ServiceManager : MonoBehaviour
    {
        CloudAPIService cloudAPIService;

        private void Awake()
        {
            GamioAppContext.Register<ILoginEvents>(new LoginEvents());
            GamioAppContext.Register<IUIEvents>(new UIEvents());
            GamioAppContext.Register(cloudAPIService = new CloudAPIService());
            GamioAppContext.Register(new AuthService(cloudAPIService));
        }
    }
}
