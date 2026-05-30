using System.Collections.Generic;
using System.Threading.Tasks;
using Gamio.Core;
using Gamio.Services;
using Google;
using UnityEngine;

public class GoogleLoginManager : MonoBehaviour
{
    GoogleAuthManager googleAuthManager;
    private GoogleSignInConfiguration configuration;
    string webClientId = "872728126352-hcr19509f88ne1cga5im912ph8k0td9n.apps.googleusercontent.com";
    private ILoginEvents loginEvents;

    private void Awake()
    {
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            RequestEmail = true,
            RequestProfile = true
        };
    }

    void Start()
    {
        googleAuthManager = GamioAppContext.Get<GoogleAuthManager>();
    }

    private void OnEnable()
    {
        loginEvents = GamioAppContext.Get<ILoginEvents>();

        if (loginEvents != null)
        {
            loginEvents.OnSilentLoginRequested += OnSignInSilently;
            loginEvents.OnLoginRequested += OnSignIn;
        }
    }

    private void OnDisable()
    {
        if (loginEvents != null)
        {
            loginEvents.OnSilentLoginRequested -= OnSignInSilently;
            loginEvents.OnLoginRequested -= OnSignIn;
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
        GoogleSignIn.DefaultInstance.SignOut();
    }

    public void OnDisconnect()
    {
        GoogleSignIn.DefaultInstance.Disconnect();
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
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
        }
    }

    void OnEditorAuthenticationFinished(string token)
    {
        loginEvents?.LoginSuccessful(new GoogleSignInUser
        {
            IdToken = token,
            DisplayName = "Editor User",
            Email = "editor@example.com"
        });

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
        if (task.IsFaulted || task.IsCanceled)
        {
            loginEvents?.LoginFailed("");
        }
        else
        {
            loginEvents?.LoginSuccessful(task.Result);
        }
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
