using System;
using Google;

public interface ILoginEvents : IDisposable
{
    event Action OnLoginRequested;
    event Action OnSilentLoginRequested;
    event Action OnLogoutRequested;
    event Action<GoogleSignInUser> OnLoginSuccess;
    event Action<string> OnLoginFailed;

    void RequestLogin();
    void RequestSilentLogin();
    void RequestLogout();
    void LoginSuccessful(GoogleSignInUser googleSignIn);
    void LoginFailed(string error);
}

public class LoginEvents : ILoginEvents
{
    public event Action OnLoginRequested;
    public event Action OnSilentLoginRequested;
    public event Action OnLogoutRequested;
    public event Action<GoogleSignInUser> OnLoginSuccess;
    public event Action<string> OnLoginFailed;

    public void RequestLogin() => OnLoginRequested?.Invoke();
    public void LoginFailed(string error) => OnLoginFailed?.Invoke(error);
    public void LoginSuccessful(GoogleSignInUser googleSignIn) => OnLoginSuccess?.Invoke(googleSignIn);
    public void RequestLogout() => OnLogoutRequested?.Invoke();
    public void RequestSilentLogin() => OnSilentLoginRequested?.Invoke();

    public void Dispose()
    {
        OnLoginRequested = null;
        OnSilentLoginRequested = null;
        OnLogoutRequested = null;
        OnLoginSuccess = null;
        OnLoginFailed = null;
    }
}
