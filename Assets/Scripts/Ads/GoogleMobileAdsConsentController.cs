using Gamio.Features.Popup;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace GoogleMobileAds.Samples
{
    public class GoogleMobileAdsConsentController : MonoBehaviour
    {
        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        [SerializeField, Tooltip("Button to show user consent and privacy settings.")]
        private Button _privacyButton;

        public void GatherConsent(Action<string> onComplete)
        {
            Debug.Log("Gathering consent.");

            var requestParameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false,
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.Disabled,
                    TestDeviceHashedIds = GoogleMobileAdsController.TestDeviceIds,
                }
            };

            onComplete = (onComplete == null)
                ? ShowError
                : onComplete + ShowError;

            ConsentInformation.Update(requestParameters, (FormError updateError) =>
            {
                UpdatePrivacyButton();

                if (updateError != null)
                {
                    onComplete(updateError.Message);
                    return;
                }

                if (CanRequestAds)
                {
                    onComplete(null);
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
                {
                    UpdatePrivacyButton();
                    if (showError != null)
                    {
                        onComplete?.Invoke(showError.Message);
                    }
                    else
                    {
                        onComplete?.Invoke(null);
                    }
                });
            });
        }

        public void ShowPrivacyOptionsForm(Action<string> onComplete)
        {
            Debug.Log("Showing privacy options form.");

            onComplete = (onComplete == null)
                ? ShowError
                : onComplete + ShowError;

            ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
            {
                UpdatePrivacyButton();
                if (showError != null)
                {
                    onComplete?.Invoke(showError.Message);
                }
                else
                {
                    onComplete?.Invoke(null);
                }
            });
        }

        public void ResetConsentInformation()
        {
            ConsentInformation.Reset();
            UpdatePrivacyButton();
        }

        void UpdatePrivacyButton()
        {
            if (_privacyButton != null)
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    _privacyButton.interactable =
                        ConsentInformation.PrivacyOptionsRequirementStatus ==
                            PrivacyOptionsRequirementStatus.Required;
                });
            }
        }

        void ShowError(string message)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (string.IsNullOrEmpty(message)) return;
                PopupUI.Show("Error", message, confirmLabel: "OK");
            });
        }
    }
}
