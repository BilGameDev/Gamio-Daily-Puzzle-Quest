using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Gamio.UI;

namespace Gamio.Features.Popup
{
    public class SettingsPopupUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private SliderToggle audioToggle;
        [SerializeField] private TextMeshProUGUI audioLabel;
        [SerializeField] private SliderToggle hapticsToggle;
        [SerializeField] private TextMeshProUGUI hapticsLabel;
        [SerializeField] private Button creditsButton;
        [SerializeField] private TextMeshProUGUI creditsButtonLabel;
        [SerializeField] private Button closeButton;

        private const string AudioPrefKey = "Gamio_AudioEnabled";
        private const string HapticsPrefKey = "Gamio_HapticsEnabled";

        public static SettingsPopupUI Show()
        {
            var prefab = Resources.Load<SettingsPopupUI>("Popups/SettingsPopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("SettingsPopupUI prefab not found at Resources/Popups/SettingsPopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            popup.Setup();
            return popup;
        }

        private void Setup()
        {
            audioToggle.SetIsOn(PlayerPrefs.GetInt(AudioPrefKey, 1) == 1, true);
            hapticsToggle.SetIsOn(PlayerPrefs.GetInt(HapticsPrefKey, 1) == 1, true);

            audioToggle.OnValueChanged += OnAudioChanged;
            hapticsToggle.OnValueChanged += OnHapticsChanged;
            creditsButton.onClick.AddListener(OnCreditsClicked);
            closeButton.onClick.AddListener(Close);

            AnimateIn();
        }

        private void AnimateIn()
        {
            if (overlayGroup != null)
                overlayGroup.DOFade(1f, 0.2f).From(0f);

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.transform.localScale = Vector3.one * 0.8f;
                panelGroup.DOFade(1f, 0.2f);
                panelGroup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }
        }

        private void OnAudioChanged(bool enabled)
        {
            PlayerPrefs.SetInt(AudioPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnHapticsChanged(bool enabled)
        {
            PlayerPrefs.SetInt(HapticsPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnCreditsClicked()
        {
            CreditsPopupUI.Show();
        }

        public void Close()
        {
            audioToggle.OnValueChanged -= OnAudioChanged;
            hapticsToggle.OnValueChanged -= OnHapticsChanged;
            creditsButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();

            var seq = DOTween.Sequence();
            if (overlayGroup != null)
                seq.Join(overlayGroup.DOFade(0f, 0.15f));
            if (panelGroup != null)
                seq.Join(panelGroup.DOFade(0f, 0.15f));
            seq.OnComplete(() =>
            {
                if (this != null)
                    Destroy(gameObject);
            });
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
