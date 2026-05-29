using Gamio.Core;
using UnityEngine;

public class GamioManager : MonoBehaviour
{
    int streakCount;
    bool dailyCompleted;

    public int StreakCount => streakCount;
    public bool DailyCompleted => dailyCompleted;

    void Awake()
    {
        GamioAppContext.Register(this);
    }
}
