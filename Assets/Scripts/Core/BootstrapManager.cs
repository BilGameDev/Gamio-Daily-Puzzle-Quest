using Gamio.Core;
using Google;
using UnityEngine;

public class BootstrapManager : MonoBehaviour
{
    private GamioManager gamioManager;
    private ILoginEvents loginEvents;

    private void OnEnable()
    {
        loginEvents = GamioAppContext.Get<ILoginEvents>();

        if (loginEvents != null)
        {
            loginEvents.OnLoginSuccess += LoginSuccessful;
        }
    }

    private void Start()
    {
        gamioManager = GamioAppContext.Get<GamioManager>();

        loginEvents?.RequestSilentLogin();
    }

    private void OnDisable()
    {
        if (loginEvents != null)
        {
            loginEvents.OnLoginSuccess -= LoginSuccessful;
        }
    }

    private void LoginSuccessful(GoogleSignInUser signInUser)
    {
        gamioManager.SetGoogleUser(signInUser);
    }
}
