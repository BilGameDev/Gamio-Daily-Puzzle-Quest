using Gamio.Core;
using Gamio.Core.Services;
using Google;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamioSceneManager : MonoBehaviour
{
    [SerializeField, Scene] string bootstrapScene;
    [SerializeField, Scene] string loginScene;
    [SerializeField, Scene] string homeScene;

    private ILoginEvents loginEvents;

    private void OnEnable()
    {
        loginEvents = GamioAppContext.Get<ILoginEvents>();

        if (loginEvents != null)
        {
            loginEvents.OnLoginSuccess += LoginSuccessful;
            loginEvents.OnLoginFailed += LoginFailed;
            loginEvents.OnLogoutRequested += Logout;
        }
    }

    private void OnDisable()
    {
        if (loginEvents != null)
        {
            loginEvents.OnLoginSuccess -= LoginSuccessful;
            loginEvents.OnLoginFailed -= LoginFailed;
            loginEvents.OnLogoutRequested -= Logout;
        }
    }

    private void LoginSuccessful(GoogleSignInUser signInUser)
    {
        if (GetActiveScene() == bootstrapScene || GetActiveScene() == loginScene)
        {
            SceneLoader.LoadScene(homeScene);
            return;
        }
    }

    private void LoginFailed(string error)
    {
        if (GetActiveScene() == bootstrapScene)
        {
            SceneLoader.LoadScene(loginScene);
            return;
        }
    }

    private void Logout()
    {
        if (GetActiveScene() != loginScene)
        {
            SceneLoader.LoadScene(loginScene);
        }
    }

    string GetActiveScene() => SceneManager.GetActiveScene().path;
}
