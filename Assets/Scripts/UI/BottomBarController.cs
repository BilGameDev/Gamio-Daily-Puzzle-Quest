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

        private void Awake()
        {
            if (resetButton != null)
                resetButton.onClick.AddListener(() => GamioEvents.RequestReset());

            if (hintButton != null)
                hintButton.onClick.AddListener(() => GamioEvents.RequestHint());
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