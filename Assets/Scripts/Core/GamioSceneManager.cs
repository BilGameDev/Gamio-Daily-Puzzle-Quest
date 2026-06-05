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
    private ICloudDataEvents cloudDataEvents;
    private IUIEvents uIEvents;

    private void OnEnable()
    {
        loginEvents = GamioAppContext.Get<ILoginEvents>();
        cloudDataEvents = GamioAppContext.Get<ICloudDataEvents>();
        uIEvents = GamioAppContext.Get<IUIEvents>();

        if (loginEvents != null)
        {
            loginEvents.OnLoginFailed += LoginFailed;
            loginEvents.OnAuthFailed += LoginFailed;
            loginEvents.OnLogoutRequested += Logout;
        }

        if (cloudDataEvents != null)
        {
            cloudDataEvents.OnAllDataFetched += LoadHomeScene;
        }

        if (uIEvents != null)
        {
            uIEvents.OnGameSceneRequested += LoadGameScene;
            uIEvents.OnBackRequested += LoadHomeScene;
        }
    }

    private void OnDisable()
    {
        if (loginEvents != null)
        {
            loginEvents.OnLoginFailed -= LoginFailed;
            loginEvents.OnAuthFailed -= LoginFailed;
            loginEvents.OnLogoutRequested -= Logout;
        }

        if (cloudDataEvents != null)
        {
            cloudDataEvents.OnAllDataFetched -= LoadHomeScene;
        }

        if (uIEvents != null)
        {
            uIEvents.OnGameSceneRequested -= LoadGameScene;
            uIEvents.OnBackRequested -= LoadHomeScene;
        }
    }

    private void LoadHomeScene()
    {
        SceneLoader.LoadScene(homeScene, false);
    }

    private void LoginFailed(string error)
    {
        if (GetActiveScene() == bootstrapScene)
        {
            SceneLoader.LoadScene(loginScene);
        }
    }
    private void LoadGameScene(string gameScene)
    {
        if (!string.IsNullOrEmpty(gameScene))
        {
            SceneLoader.LoadScene(gameScene, false);
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
