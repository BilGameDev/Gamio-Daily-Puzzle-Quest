using Gamio.Core;
using Gamio.Core.Services;
using Gamio.Services;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    CloudAPIService cloudAPIService;

    void OnEnable()
    {
        GoogleAuthManager.OnGoogleAuthTokenReceived += OnLogin;
    }

    void OnDisable()
    {
        GoogleAuthManager.OnGoogleAuthTokenReceived -= OnLogin;
    }

    void OnLogin(string token)
    {
        if (cloudAPIService == null)
            cloudAPIService = new CloudAPIService();

        cloudAPIService.VerifyGoogleToken(token, OnVerifiedToken, OnError);
    }

    void OnVerifiedToken(AuthResult authResult)
    {
        cloudAPIService.SetSessionToken(authResult.sessionToken);
        
        cloudAPIService.GetSeeds(response =>
        {
            Debug.Log(JsonUtility.ToJson(response));
        }, error =>
        {
            Debug.LogWarning($"[Bootstrapper] Failed to fetch seeds: {error}");
        });
    }

    void OnError(string error)
    {
        Debug.Log(error);
    }
}
