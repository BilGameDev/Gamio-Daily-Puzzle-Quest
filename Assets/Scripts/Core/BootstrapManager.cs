using Gamio.Core;
using UnityEngine;

public class BootstrapManager : MonoBehaviour
{
    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        GamioAppContext.Get<ILoginEvents>()?.RequestSilentLogin();
    }
}
