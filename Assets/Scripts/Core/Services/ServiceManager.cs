using UnityEngine;

namespace Gamio.Core
{
    [DefaultExecutionOrder(-100)] // Make sure it runs before other managers
    public class ServiceManager : MonoBehaviour
    {
        private void Awake()
        {
            GamioAppContext.Register<ILoginEvents>(new LoginEvents());
            GamioAppContext.Register<IUIEvents>(new UIEvents());
        }
    }
}
