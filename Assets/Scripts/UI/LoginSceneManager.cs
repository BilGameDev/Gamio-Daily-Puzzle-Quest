using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gamio.Core
{
    public class LoginSceneManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform logoGraphic;
        [SerializeField] Button loginButton;
        [SerializeField] GameObject loadingOverlay;
        [SerializeField] TextMeshProUGUI statusText;

        void Awake()
        {
            loginButton.onClick.AddListener(Login);
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
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
            if (loadingOverlay != null) loadingOverlay.SetActive(true);
            if (statusText != null) statusText.text = "Signing in...";
            GamioAppContext.Get<ILoginEvents>()?.RequestLogin();
        }

        public void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        public void LoginComplete()
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
        }

        void OnDestroy()
        {
            loginButton.onClick.RemoveAllListeners();
        }
    }
}