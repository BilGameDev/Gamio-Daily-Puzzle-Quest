using Gamio.Core;
using Gamio.Features.Popup;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using System;
using UnityEngine;

namespace GoogleMobileAds.Samples
{
    public class GoogleMobileAdsConsentController : MonoBehaviour
    {
        public bool CanRequestAds => ConsentInformation.CanRequestAds();
        IUIEvents uiEvents;
        Action onPrivacyHandler;

        void OnEnable()
        {
            uiEvents = GamioAppContext.Get<IUIEvents>();
            onPrivacyHandler = () => ShowPrivacyOptionsForm();
            if (uiEvents != null)
                uiEvents.OnPrivacyRequested += onPrivacyHandler;
        }

        void OnDisable()
        {
            if (uiEvents != null && onPrivacyHandler != null)
                uiEvents.OnPrivacyRequested -= onPrivacyHandler;
        }

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
                    if (showError != null)
                        onComplete?.Invoke(showError.Message);
                    else
                        onComplete?.Invoke(null);
                });
            });
        }

        public void ShowPrivacyOptionsForm(Action<string> onComplete = null)
        {
            Debug.Log("Showing privacy options form.");

            onComplete = onComplete ?? ShowError;
            ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
            {
                if (showError != null)
                    onComplete?.Invoke(showError.Message);
            });
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
