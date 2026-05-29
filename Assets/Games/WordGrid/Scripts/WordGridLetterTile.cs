using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Gamio.Games.WordGrid
{
    [RequireComponent(typeof(Image))]
    public class WordGridLetterTile : MonoBehaviour, IPointerClickHandler
    {
        [NonSerialized] public char Letter;

        private Image image;
        private TextMeshProUGUI letterText;

        public Image Image
        {
            get
            {
                if (image == null) image = GetComponent<Image>();
                return image;
            }
        }

        public event Action<char> OnClicked;

        private void Awake()
        {
            image = GetComponent<Image>();
            letterText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (letterText == null)
            {
                var textGO = new GameObject("Text", typeof(RectTransform));
                textGO.transform.SetParent(transform, false);
                var textRT = textGO.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                letterText = textGO.AddComponent<TextMeshProUGUI>();
                letterText.fontSize = 30;
                letterText.alignment = TextAlignmentOptions.Center;
                letterText.color = Color.white;
                if (TMP_Settings.defaultFontAsset != null)
                    letterText.font = TMP_Settings.defaultFontAsset;
                letterText.raycastTarget = false;
            }
            else
            {
                letterText.raycastTarget = false;
            }
        }

        public void Init(char letter)
        {
            Letter = letter;
            if (letterText != null)
            {
                letterText.text = letter.ToString();
                letterText.gameObject.SetActive(true);
                letterText.color = Color.white;
            }
            Image.color = WordGridGame.ActiveSettings != null ? WordGridGame.ActiveSettings.TileDefaultColor : Color.gray;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 1, 0.5f);
            OnClicked?.Invoke(Letter);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
