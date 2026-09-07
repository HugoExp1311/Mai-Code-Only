using UnityEngine;
using UnityEngine.UI;
using Base;
using Base.Localization;
using System;
using System.Collections.Generic;
using TMPro;

namespace UI
{
    /// <summary>
    /// Simple confirm popup with Yes/No/Close buttons.
    /// Used for confirmation dialogs like "Return to Title?".
    /// </summary>
    public class ConfirmPopup : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private LocalizedText questionLocalizedText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Button closeButton;
        
        private Action onYesCallback;
        private Action onNoCallback;
        private Action onCloseCallback;
        private string defaultYesButtonText;
        private string defaultNoButtonText;
        private bool defaultNoButtonVisible = true;
        private bool buttonDefaultsCaptured;
        
        protected override void OnShow(object data)
        {
            base.OnShow(data);

            EnsureButtonDefaultsCaptured();
            ResetPopupDisplay();
            
            // Setup button listeners
            if (yesButton != null)
            {
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(OnYesClicked);
            }
            
            if (noButton != null)
            {
                noButton.onClick.RemoveAllListeners();
                noButton.onClick.AddListener(OnNoClicked);
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCloseClicked);
            }
            
            // Handle data if provided
            if (data is ConfirmPopupData popupData)
            {
                SetupFromData(popupData);
            }
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            // Clear callbacks
            onYesCallback = null;
            onNoCallback = null;
            onCloseCallback = null;
        }
        
        /// <summary>
        /// Setup the popup from ConfirmPopupData
        /// </summary>
        private void SetupFromData(ConfirmPopupData data)
        {
            if (questionLocalizedText != null)
            {
                if (!string.IsNullOrEmpty(data.questionDirectText))
                {
                    questionLocalizedText.SetDirect(data.questionDirectText);
                }
                else if (!string.IsNullOrEmpty(data.questionLocalizationKey))
                {
                    questionLocalizedText.SetLocalized(data.questionLocalizationKey, LocalizationDomains.UI, data.questionVariables);
                }
            }

            if (!string.IsNullOrEmpty(data.yesButtonLocalizationKey))
            {
                SetButtonLocalizedText(yesButton, data.yesButtonLocalizationKey, data.yesButtonVariables);
            }
            else
            {
                SetButtonDirectText(yesButton, data.yesButtonText);
            }

            if (!string.IsNullOrEmpty(data.noButtonLocalizationKey))
            {
                SetButtonLocalizedText(noButton, data.noButtonLocalizationKey, data.noButtonVariables);
            }
            else if (!string.IsNullOrEmpty(data.noButtonText))
            {
                SetButtonDirectText(noButton, data.noButtonText);
            }

            SetButtonVisible(noButton, data.showNoButton);

            // Store callbacks
            onYesCallback = data.onYes;
            onNoCallback = data.onNo;
            onCloseCallback = data.onClose;
        }
        
        /// <summary>
        /// Set the question text using a localization key
        /// </summary>
        public void SetQuestionKey(string localizationKey)
        {
            if (questionLocalizedText != null)
            {
                questionLocalizedText.SetLocalized(localizationKey, LocalizationDomains.UI);
            }
        }
        
        /// <summary>
        /// Set callbacks for button actions
        /// </summary>
        public void SetCallbacks(Action onYes, Action onNo = null, Action onClose = null)
        {
            onYesCallback = onYes;
            onNoCallback = onNo;
            onCloseCallback = onClose;
        }
        
        private void OnYesClicked()
        {
            PlayButtonSound();
            onYesCallback?.Invoke();
            Hide();
        }
        
        private void OnNoClicked()
        {
            PlayButtonSound();
            onNoCallback?.Invoke();
            Hide();
        }
        
        private void OnCloseClicked()
        {
            PlayButtonSound();
            onCloseCallback?.Invoke();
            Hide();
        }
        
        private void PlayButtonSound()
        {
            if (AudioManager.Instance != null && FMODEvents.Instance != null)
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnButton);
            }
        }

        private void EnsureButtonDefaultsCaptured()
        {
            if (buttonDefaultsCaptured)
            {
                return;
            }

            defaultYesButtonText = GetButtonText(yesButton);
            defaultNoButtonText = GetButtonText(noButton);
            defaultNoButtonVisible = noButton == null || noButton.gameObject.activeSelf;
            buttonDefaultsCaptured = true;
        }

        private void ResetPopupDisplay()
        {
            ResetQuestionText();
            ResetButtonText(yesButton, defaultYesButtonText);
            ResetButtonText(noButton, defaultNoButtonText);
            SetButtonVisible(noButton, defaultNoButtonVisible);
        }

        private void ResetQuestionText()
        {
            if (questionLocalizedText != null && !string.IsNullOrEmpty(questionLocalizedText.Key))
            {
                questionLocalizedText.SetLocalized(questionLocalizedText.Key, questionLocalizedText.TableName);
            }
        }

        private static string GetButtonText(Button button)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            return text != null ? text.text : string.Empty;
        }

        private static void ResetButtonText(Button button, string fallbackText)
        {
            if (button == null)
            {
                return;
            }

            LocalizedText localizedText = button.GetComponentInChildren<LocalizedText>(true);
            if (localizedText != null && !string.IsNullOrEmpty(localizedText.Key))
            {
                localizedText.SetLocalized(localizedText.Key, localizedText.TableName);
                return;
            }

            SetRawButtonText(button, fallbackText);
        }

        private static void SetButtonDirectText(Button button, string text)
        {
            if (button == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            LocalizedText localizedText = button.GetComponentInChildren<LocalizedText>(true);
            if (localizedText != null)
            {
                localizedText.SetDirect(text);
                return;
            }

            SetRawButtonText(button, text);
        }

        private static void SetButtonLocalizedText(Button button, string key, Dictionary<string, object> variables = null)
        {
            if (button == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            LocalizedText localizedText = button.GetComponentInChildren<LocalizedText>(true);
            if (localizedText != null)
            {
                localizedText.SetLocalized(key, LocalizationDomains.UI, variables);
            }
        }

        private static void SetRawButtonText(Button button, string text)
        {
            TMP_Text tmpText = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (tmpText != null)
            {
                tmpText.text = text ?? string.Empty;
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
    
    /// <summary>
    /// Data class for configuring ConfirmPopup
    /// </summary>
    public class ConfirmPopupData
    {
        public string questionLocalizationKey;
        public Dictionary<string, object> questionVariables;
        public string questionDirectText;
        public string yesButtonText;
        public string yesButtonLocalizationKey;
        public Dictionary<string, object> yesButtonVariables;
        public string noButtonText;
        public string noButtonLocalizationKey;
        public Dictionary<string, object> noButtonVariables;
        public bool showNoButton = true;
        public Action onYes;
        public Action onNo;
        public Action onClose;
        
        public ConfirmPopupData(string questionKey, Action yesCallback, Action noCallback = null, Action closeCallback = null)
        {
            questionLocalizationKey = questionKey;
            onYes = yesCallback;
            onNo = noCallback;
            onClose = closeCallback;
        }

        public static ConfirmPopupData CreateLocalized(
            string questionKey,
            Dictionary<string, object> questionVariables,
            string yesButtonKey,
            Action yesCallback,
            bool showNoButton = true,
            Dictionary<string, object> yesButtonVariables = null,
            string noButtonKey = null,
            Dictionary<string, object> noButtonVariables = null,
            Action noCallback = null,
            Action closeCallback = null)
        {
            return new ConfirmPopupData(questionKey, yesCallback, noCallback, closeCallback)
            {
                questionVariables = questionVariables,
                yesButtonLocalizationKey = yesButtonKey,
                yesButtonVariables = yesButtonVariables,
                noButtonLocalizationKey = noButtonKey,
                noButtonVariables = noButtonVariables,
                showNoButton = showNoButton
            };
        }

        public static ConfirmPopupData CreateDirect(
            string questionText,
            string yesText,
            Action yesCallback,
            bool showNoButton = true,
            string noText = null,
            Action noCallback = null,
            Action closeCallback = null)
        {
            return new ConfirmPopupData(null, yesCallback, noCallback, closeCallback)
            {
                questionDirectText = questionText,
                yesButtonText = yesText,
                noButtonText = noText,
                showNoButton = showNoButton
            };
        }
    }
}
