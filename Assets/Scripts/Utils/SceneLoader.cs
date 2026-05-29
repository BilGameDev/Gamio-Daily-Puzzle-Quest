using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Gamio.Core.Services
{
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;
        [SerializeField] private CanvasGroup _overlay;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private float _minDisplayTime = 0.8f;

        private float _showTime;
        private Coroutine _pendingHide;
        private bool _isLoadingScene;
        private string _currentSceneName;

        public static bool IsShowing => _instance != null && _instance._overlay.alpha > 0f;

        public static void Show(string text = "Loading...")
        {
            EnsureInstance();
            SetOverlay(text);
        }

        public static void LoadScene(string sceneName, string loadingText = "Loading...", System.Action onCompleted = null)
        {
            EnsureInstance();
            if (_instance._isLoadingScene && _instance._currentSceneName == sceneName) return;
            _instance.StartCoroutine(_instance.LoadSceneRoutine(sceneName, loadingText, onCompleted));
        }

        public static void Hide()
        {
            if (_instance == null) return;

            if (_instance._pendingHide != null)
            {
                _instance.StopCoroutine(_instance._pendingHide);
                _instance._pendingHide = null;
            }

            var elapsed = Time.realtimeSinceStartup - _instance._showTime;
            if (elapsed < _instance._minDisplayTime)
            {
                _instance._pendingHide = _instance.StartCoroutine(
                    _instance.HideAfterDelay(_instance._minDisplayTime - elapsed));
            }
            else
            {
                DoHide();
            }
        }

        private static void SetOverlay(string text)
        {
            _instance._loadingText.text = text;
            _instance._overlay.gameObject.SetActive(true);
            _instance._overlay.alpha = 0f;
            _instance._overlay.DOKill();
            _instance._overlay.DOFade(1f, 0.15f);
            _instance._showTime = Time.realtimeSinceStartup;
        }

        private IEnumerator LoadSceneRoutine(string sceneName, string loadingText, System.Action onCompleted = null)
        {
            _isLoadingScene = true;
            _currentSceneName = sceneName;
            yield return new WaitForSecondsRealtime(0.25f);
            DG.Tweening.DOTween.KillAll();
            SetOverlay(loadingText);

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone)
                yield return null;

            yield return null;

            var elapsed = Time.realtimeSinceStartup - _showTime;
            if (elapsed < _minDisplayTime)
                yield return new WaitForSecondsRealtime(_minDisplayTime - elapsed);

            _isLoadingScene = false;
            DoHide();
            onCompleted?.Invoke();
        }

        private static void DoHide()
        {
            _instance._overlay.DOKill();
            _instance._overlay.DOFade(0f, 0.15f).OnComplete(() =>
            {
                _instance._overlay.gameObject.SetActive(false);
            });
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _pendingHide = null;
            DoHide();
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var prefab = Resources.Load<SceneLoader>("Popups/SceneLoaderCanvas");
            if (prefab != null)
            {
                _instance = Instantiate(prefab);
            }
            else
            {
                var go = new GameObject("SceneLoaderCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                _instance = go.AddComponent<SceneLoader>();
                _instance.CreateDefaultUI();
            }

            DontDestroyOnLoad(_instance.gameObject);
            _instance._overlay.gameObject.SetActive(false);
        }

        private void CreateDefaultUI()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var overlayObj = new GameObject("Overlay", typeof(RectTransform));
            overlayObj.transform.SetParent(transform, false);
            var rt = overlayObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = overlayObj.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.7f);

            _overlay = overlayObj.AddComponent<CanvasGroup>();
            _overlay.alpha = 0f;

            var loadingObj = new GameObject("LoadingText", typeof(RectTransform));
            loadingObj.transform.SetParent(overlayObj.transform, false);
            var lrt = loadingObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(400, 80);

            _loadingText = loadingObj.AddComponent<TextMeshProUGUI>();
            _loadingText.text = "Loading...";
            _loadingText.fontSize = 32;
            _loadingText.alignment = TextAlignmentOptions.Center;
            _loadingText.color = Color.white;
            _loadingText.fontStyle = FontStyles.Bold;
            if (TMP_Settings.defaultFontAsset != null)
                _loadingText.font = TMP_Settings.defaultFontAsset;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
