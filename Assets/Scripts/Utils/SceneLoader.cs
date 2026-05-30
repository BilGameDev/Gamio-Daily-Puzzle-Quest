using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gamio.Core.Services
{
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;
        [SerializeField] private CanvasGroup _overlay;
        [SerializeField] private float _fadeDuration = 0.25f;

        private bool _isLoading;

        public static void LoadScene(string sceneName, System.Action onCompleted = null)
        {
            EnsureInstance();
            if (_instance._isLoading) return;
            
            _instance.StartCoroutine(_instance.LoadSceneRoutine(sceneName, onCompleted));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, System.Action onCompleted)
        {
            _isLoading = true;
            _overlay.gameObject.SetActive(true);

            // 1. Smooth Fade In
            yield return _overlay.DOFade(1f, _fadeDuration).WaitForCompletion();

            // 2. Kill local UI tweens right before destroying the active scene layout
            DOTween.KillAll(); 

            // 3. Load the Scene Async and wait until complete
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!asyncOp.isDone)
                yield return null;

            // 4. Smooth Fade Out
            yield return _overlay.DOFade(0f, _fadeDuration).WaitForCompletion();
            
            _overlay.gameObject.SetActive(false);
            _isLoading = false;
            
            onCompleted?.Invoke();
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var prefab = Resources.Load<SceneLoader>("Popups/SceneLoaderCanvas");
            if (prefab == null)
            {
                Debug.LogError("[SceneLoader] Popups/SceneLoaderCanvas not found in Resources.");
                return;
            }

            _instance = Instantiate(prefab);
            DontDestroyOnLoad(_instance.gameObject);
            
            // Ensure starting baseline state
            _instance._overlay.alpha = 0f;
            _instance._overlay.gameObject.SetActive(false);
        }
    }
}
