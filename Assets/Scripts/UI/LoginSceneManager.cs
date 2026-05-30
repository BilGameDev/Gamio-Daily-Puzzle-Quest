using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Core
{
    public class LoginSceneManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform logoGraphic;
        [SerializeField] Button loginButton;

        void Awake()
        {
            loginButton.onClick.AddListener(Login);
        }

        void Start()
        {
            logoGraphic.localScale = Vector3.zero;
            logoGraphic.DOScale(1f, 0.6f).SetEase(Ease.OutBack);

            loginButton.transform.localScale = Vector3.zero;
            loginButton.transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack).SetDelay(0.2f);
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