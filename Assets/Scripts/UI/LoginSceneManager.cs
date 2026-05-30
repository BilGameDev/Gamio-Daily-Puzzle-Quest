using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Core
{
    public class LoginSceneManager : MonoBehaviour
    {
        [SerializeField] Button loginButton;
        void Awake()
        {
            loginButton.onClick.AddListener(Login);
        }

        void Login()
        {
            GamioAppContext.Get<ILoginEvents>()?.RequestLogin();
        }

        void OnDestroy()
        {
            loginButton.onClick.RemoveAllListeners();
        }
    }
}