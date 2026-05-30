using Gamio.Core;
using Google;
using UnityEngine;

public class GamioManager : MonoBehaviour
{
    int streakCount;
    bool dailyCompleted;
    bool challengeActive;
    GoogleSignInUser signInUser;

    public int StreakCount => streakCount;
    public bool DailyCompleted => dailyCompleted;
    public bool IsChallengeActive => challengeActive;
    public GoogleSignInUser GoogleUser => signInUser;


    void Awake()
    {
        GamioAppContext.Register(this);
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void SetGoogleUser(GoogleSignInUser googleSignInUser)
    {
        signInUser = googleSignInUser;
    }

    public void SetChallengeActive(bool active)
    {
        challengeActive = active;
    }
}
