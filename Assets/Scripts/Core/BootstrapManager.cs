using Gamio.Core;
using UnityEngine;

public class BootstrapManager : MonoBehaviour
{
    private void Start()
    {
        GamioAppContext.Get<ILoginEvents>()?.RequestSilentLogin();
    }
}
