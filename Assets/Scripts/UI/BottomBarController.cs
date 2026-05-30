using Gamio.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gamio.Features.UI
{
    public class BottomBarController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button hintButton;

        IUIEvents uIEvents;

        void Start()
        {
            uIEvents = GamioAppContext.Get<IUIEvents>();

            if (uIEvents != null)
            {
                if (resetButton != null)
                    resetButton.onClick.AddListener(() => uIEvents.RequestReset());

                if (hintButton != null)
                    hintButton.onClick.AddListener(() => uIEvents.RequestHint());
            }
        }

        private void OnDestroy()
        {
            if (resetButton != null)
                resetButton.onClick.RemoveAllListeners();

            if (hintButton != null)
                hintButton.onClick.RemoveAllListeners();
        }
    }
}