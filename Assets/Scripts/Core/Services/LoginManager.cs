using System.Collections.Generic;
using System.Threading.Tasks;
using Gamio.Core;
using Gamio.Core.Services;
using Gamio.Services;
using Google;
using UnityEngine;

public class LoginManager : MonoBehaviour
{
    GoogleAuthManager googleAuthManager;
    private GoogleSignInConfiguration configuration;
    string webClientId;
    private ILoginEvents loginEvents;
    private AuthService authService;

    private void Awake()
    {
        googleAuthManager = GamioAppContext.Get<GoogleAuthManager>();
        webClientId = GameSecretsLoader.Load().googleWebClientId;
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            RequestEmail = true,
            RequestProfile = true
        };
    }

    private void OnEnable()
    {
        loginEvents = GamioAppContext.Get<ILoginEvents>();
        authService = GamioAppContext.Get<AuthService>();

        if (loginEvents != null)
        {
            loginEvents.OnSilentLoginRequested += OnSignInSilently;
            loginEvents.OnLoginRequested += OnSignIn;
            loginEvents.OnLogoutRequested += OnSignOut;
        }
    }

    private void OnDisable()
    {
        if (loginEvents != null)
        {
            loginEvents.OnSilentLoginRequested -= OnSignInSilently;
            loginEvents.OnLoginRequested -= OnSignIn;
            loginEvents.OnLogoutRequested -= OnSignOut;
        }
    }

    public void OnSignIn()
    {
#if UNITY_EDITOR
        GoogleAuthManager.OnGoogleAuthTokenReceived += OnEditorAuthenticationFinished;
        googleAuthManager.StartLogin();

#else
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
          OnAuthenticationFinished);
#endif
    }

    public void OnSignInSilently()
    {

#if UNITY_EDITOR
        GoogleAuthManager.OnGoogleAuthTokenReceived += OnEditorAuthenticationFinished;
        GoogleAuthManager.OnGoogleLoginFailed += OnEditorAuthenticationFailed;
        googleAuthManager.StartSilentLogin();

#else
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;

        GoogleSignIn.DefaultInstance.SignInSilently()
              .ContinueWith(OnSilentAuthenticationFinished);
#endif

    }

    public void OnSignOut()
    {
#if UNITY_EDITOR
        GoogleAuthManager.ClearStoredSession();
#else
        GoogleSignIn.DefaultInstance.SignOut();
#endif
        authService?.ClearSession();
    }

    public void OnDisconnect()
    {
        GoogleSignIn.DefaultInstance.Disconnect();
        authService?.ClearSession();
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        UnityMainThreadHelper.Run(() =>
        {
            if (task.IsFaulted)
            {
                HandleSignInError(task);
            }
            else if (task.IsCanceled)
            {
                loginEvents?.LoginFailed("Sign-in canceled");
            }
            else
            {
                loginEvents?.LoginSuccessful(task.Result);
                authService?.AuthenticateWithGoogle(task.Result.IdToken);
            }
        });
    }

    void OnEditorAuthenticationFinished(string token)
    {
        loginEvents?.LoginSuccessful(new GoogleSignInUser
        {
            IdToken = token,
            DisplayName = "Editor User",
            Email = "editor@example.com"
        });

        authService?.AuthenticateWithGoogle(token);

        GoogleAuthManager.OnGoogleAuthTokenReceived -= OnEditorAuthenticationFinished;
    }

    void OnEditorAuthenticationFailed(string error)
    {
        GoogleAuthManager.OnGoogleAuthTokenReceived -= OnEditorAuthenticationFinished;
        GoogleAuthManager.OnGoogleLoginFailed -= OnEditorAuthenticationFailed;
        loginEvents?.LoginFailed(error);
    }

    internal void OnSilentAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        UnityMainThreadHelper.Run(() =>
        {
            if (task.IsFaulted)
            {
                HandleSignInError(task);
            }
            else if (task.IsCanceled)
            {
                loginEvents?.LoginFailed("Silent sign-in canceled");
            }
            else
            {
                loginEvents?.LoginSuccessful(task.Result);
                authService?.AuthenticateWithGoogle(task.Result.IdToken);
            }
        });
    }

    private void HandleSignInError(Task<GoogleSignInUser> task)
    {
        using (IEnumerator<System.Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                loginEvents?.LoginFailed($"Google Sign-In Error: {error.Status} - {error.Message}");
            }
            else
            {
                loginEvents?.LoginFailed($"Unexpected Sign-In Exception: {task.Exception}");
            }
        }
    }
}
