using UnityEngine;

namespace Gamio.Core
{
    public class TutorialService
    {
        private const string Prefix = "Gamio_Tutorial_";

        public bool IsCompleted(string gameId)
        {
            if (GamioAppContext.Get<GamioManager>().ChallengeActive)
            {
                return true;
            }

            return PlayerPrefs.GetInt(Prefix + gameId, 0) == 1;
        }

        public void MarkCompleted(string gameId)
        {
            PlayerPrefs.SetInt(Prefix + gameId, 1);
            PlayerPrefs.Save();
        }

        public void Reset(string gameId)
        {
            PlayerPrefs.DeleteKey(Prefix + gameId);
            PlayerPrefs.Save();
        }

        public void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
