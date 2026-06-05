using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.Popup
{
    public class CreditsPopupUI : SlideUpPopup
    {
        [SerializeField] private Button closeButton;

        public static CreditsPopupUI Show()
        {
            var prefab = Resources.Load<CreditsPopupUI>("Popups/CreditsPopupCanvas");
            if (prefab == null)
            {
                Debug.LogError("CreditsPopupUI prefab not found at Resources/Popups/CreditsPopupCanvas");
                return null;
            }
            var popup = Instantiate(prefab);
            popup.Setup();
            return popup;
        }

        private void Setup()
        {
            closeButton.onClick.AddListener(Close);
            Open();
        }

        public override void Close()
        {
            closeButton.onClick.RemoveAllListeners();
            base.Close();
        }
    }
}
