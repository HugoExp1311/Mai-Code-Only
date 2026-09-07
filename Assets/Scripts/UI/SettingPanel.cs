using UnityEngine;
using UnityEngine.UI;
using Base;
using Base.Localization;
using Base.Settings;
using System;
using System.Collections;

namespace UI
{
    /// <summary>
    /// Settings panel for managing game settings including audio volumes, language, and screen mode.
    /// Extends UIPanel for consistent panel behavior.
    /// </summary>
    public class SettingPanel : UIPanel
    {
        // OPT-40: Cache Enum.GetNames once (static — shared across instances, never changes)
        private static readonly string[] _languageNames = System.Enum.GetNames(typeof(Language));
        private static readonly int _languageCount = _languageNames.Length;
        [Header("Audio Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider soundSlider;
        [SerializeField] private Slider voiceSlider;
        
        [Header("Language Selection (Left/Right Buttons + ScrollRect)")]
        [SerializeField] private Button languageLeftButton;
        [SerializeField] private Button languageRightButton;
        [SerializeField] private ScrollRect languageScrollRect;
        [SerializeField] private Scrollbar languageHorizontalScrollbar;
        
        [Header("Language Animation")]
        [SerializeField] private float scrollAnimationDuration = 0.2f;
        
        [Header("Screen Mode")]
        [SerializeField] private Button fullScreenButton;
        [SerializeField] private Button windowButton;
        
        [Header("Return to Title")]
        [SerializeField] private Button returnToTitleButton;
        
        private SettingsManager settingsManager;
        private Coroutine scrollAnimationCoroutine;
        
        protected override void OnShow(object data)
        {
            base.OnShow(data);
            
            // Get settings manager reference
            settingsManager = GameManager.Instance?.SettingsManager;
            
            if (settingsManager == null)
            {
                Debug.LogError("[SettingPanel] SettingsManager is null!");
                return;
            }
            
            LocalizationManager.Instance.LanguageChanged += HandleLocalizationChanged;
            
            // Initialize UI with current settings
            InitializeSliders();
            InitializeLanguageButtons();
            InitializeScreenButtons();
            InitializeReturnButton();
            
            // Reorder language items to match enum order
            ReorderLanguageItems();
            
            // Update UI to reflect current settings
            RefreshUI();
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            
            // Stop any running animation
            if (scrollAnimationCoroutine != null)
            {
                StopCoroutine(scrollAnimationCoroutine);
                scrollAnimationCoroutine = null;
            }
            
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.LanguageChanged -= HandleLocalizationChanged;
            
            // Save settings to PlayerPrefs when panel closes
            SaveSettings();
        }
        
        private void InitializeSliders()
        {
            // Setup Music Slider
            if (musicSlider != null)
            {
                musicSlider.minValue = 0;
                musicSlider.maxValue = 100;
                musicSlider.wholeNumbers = true;
                musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            
            // Setup Sound Slider
            if (soundSlider != null)
            {
                soundSlider.minValue = 0;
                soundSlider.maxValue = 100;
                soundSlider.wholeNumbers = true;
                soundSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);
                soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            }
            
            // Setup Voice Slider
            if (voiceSlider != null)
            {
                voiceSlider.minValue = 0;
                voiceSlider.maxValue = 100;
                voiceSlider.wholeNumbers = true;
                voiceSlider.onValueChanged.RemoveListener(OnVoiceVolumeChanged);
                voiceSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            }
        }
        
        private void InitializeLanguageButtons()
        {
            // Language uses left/right buttons to cycle through languages
            // Remove existing listeners first to prevent duplicates on re-open
            if (languageLeftButton != null)
            {
                languageLeftButton.onClick.RemoveListener(OnLanguageLeftClicked);
                languageLeftButton.onClick.AddListener(OnLanguageLeftClicked);
            }
            
            if (languageRightButton != null)
            {
                languageRightButton.onClick.RemoveListener(OnLanguageRightClicked);
                languageRightButton.onClick.AddListener(OnLanguageRightClicked);
            }
        }
        
        private void InitializeScreenButtons()
        {
            if (fullScreenButton != null)
            {
                fullScreenButton.onClick.RemoveAllListeners();
                fullScreenButton.onClick.AddListener(() => SetScreenMode(ScreenSettings.FullScreen));
            }
            
            if (windowButton != null)
            {
                windowButton.onClick.RemoveAllListeners();
                windowButton.onClick.AddListener(() => SetScreenMode(ScreenSettings.Window));
            }
        }
        
        private void InitializeReturnButton()
        {
            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.RemoveListener(OnReturnToTitleClicked);
                returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
            }
        }
        
        /// <summary>
        /// Reorder language items in the ScrollRect content to match the Language enum order.
        /// Expects children named: English, Vietnamese, Japanese
        /// </summary>
        private void ReorderLanguageItems()
        {
            if (languageScrollRect == null || languageScrollRect.content == null) return;
            
            Transform content = languageScrollRect.content;
            
            for (int i = 0; i < _languageNames.Length; i++)
            {
                Transform languageItem = content.Find(_languageNames[i]);
                if (languageItem != null)
                {
                    languageItem.SetSiblingIndex(i);
                }
            }
        }
        
        private void RefreshUI()
        {
            if (settingsManager == null) return;
            
            // Update sliders
            // OPT-7: Use typed getters instead of dynamic GetSetting()
            int musicVolume = settingsManager.GetMusicVolume();
            int soundVolume = settingsManager.GetSoundVolume();
            int voiceVolume = settingsManager.GetVoiceVolume();
            
            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(musicVolume);
            if (soundSlider != null)
                soundSlider.SetValueWithoutNotify(soundVolume);
            if (voiceSlider != null)
                voiceSlider.SetValueWithoutNotify(voiceVolume);
            
            // Update language scroll position (no animation on initial load)
            UpdateLanguageScrollPosition(false);
        }
        
        #region Language Handlers
        
        private void OnLanguageLeftClicked()
        {
            GameManager.Instance.ChangeLocale(-1);
        }
        
        private void OnLanguageRightClicked()
        {
            GameManager.Instance.ChangeLocale(1);
        }
        
        private void UpdateLanguageScrollPosition(bool animate)
        {
            if (languageHorizontalScrollbar == null) return;
            
            int currentIndex = (int)(settingsManager?.GetLanguage() ?? Language.English);
            float targetValue = (float)currentIndex / (float)(_languageCount - 1);
            
            if (animate && gameObject.activeInHierarchy)
            {
                // Stop any existing animation
                if (scrollAnimationCoroutine != null)
                {
                    StopCoroutine(scrollAnimationCoroutine);
                }
                scrollAnimationCoroutine = StartCoroutine(AnimateScrollbarValue(targetValue));
            }
            else
            {
                // Set immediately without animation
                languageHorizontalScrollbar.value = targetValue;
            }
            
            // Update button interactability based on boundaries
            UpdateLanguageButtonInteractability();
        }
        
        private IEnumerator AnimateScrollbarValue(float targetValue)
        {
            float startValue = languageHorizontalScrollbar.value;
            float elapsed = 0f;
            
            while (elapsed < scrollAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scrollAnimationDuration;
                
                // Use smooth easing (ease out cubic)
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                languageHorizontalScrollbar.value = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }
            
            // Ensure we end at exact target
            languageHorizontalScrollbar.value = targetValue;
            scrollAnimationCoroutine = null;
        }
        
        private void HandleLocalizationChanged(Language language, string localeCode, System.Globalization.CultureInfo culture)
        {
            // Animate to new position when language changes
            UpdateLanguageScrollPosition(true);
            
            // Update button interactability based on boundaries
            UpdateLanguageButtonInteractability();
        }

        /// <summary>
        /// Updates the interactability of language left/right buttons based on current language position.
        /// Disables left button at first language, disables right button at last language.
        /// </summary>
        private void UpdateLanguageButtonInteractability()
        {
            int currentIndex = (int)(settingsManager?.GetLanguage() ?? Language.English);
            int maxIndex = _languageCount - 1;
            
            // Disable left button if at first language (index 0)
            if (languageLeftButton != null)
            {
                languageLeftButton.interactable = currentIndex > 0;
            }
            
            // Disable right button if at last language (max index)
            if (languageRightButton != null)
            {
                languageRightButton.interactable = currentIndex < maxIndex;
            }
        }
        
        #endregion
        
        #region Audio Volume Handlers
        
        private void OnMusicVolumeChanged(float value)
        {
            int volume = Mathf.RoundToInt(value);
            // OPT-8: Use typed setter instead of dynamic UpdateSettings()
            settingsManager?.UpdateMusicVolume(volume);
        }
        
        private void OnSoundVolumeChanged(float value)
        {
            int volume = Mathf.RoundToInt(value);
            settingsManager?.UpdateSoundVolume(volume);
        }
        
        private void OnVoiceVolumeChanged(float value)
        {
            int volume = Mathf.RoundToInt(value);
            settingsManager?.UpdateVoiceVolume(volume);
        }
        
        #endregion
        
        #region Screen Mode Handlers
        
        private void SetScreenMode(ScreenSettings screenMode)
        {
            if (settingsManager == null) return;
            
            // Update settings
            settingsManager.UpdateScreenSettings(screenMode);
            
            // Apply screen mode
            switch (screenMode)
            {
                case ScreenSettings.FullScreen:
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    break;
                case ScreenSettings.Window:
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                    break;
            }
            
            // Play button sound
            PlayButtonSound();
        }
        
        #endregion
        
        #region Return to Title
        
        private void OnReturnToTitleClicked()
        {
            // Play button sound
            PlayButtonSound();
            
            // Show confirm popup
            var confirmData = new ConfirmPopupData(
                "Confirm Question In Game",
                () =>
                {
                    // Hide this panel first
                    Hide();
                    
                    // Switch to MainMenu section
                    if (UIPanelManager.Instance != null)
                    {
                        UIPanelManager.Instance.SwitchToSection(GameSection.MainMenu);
                    }
                },
                null,  // No action for No button, popup will just close
                null   // No action for Close button, popup will just close
            );
            
            // Show the confirm popup
            if (UIPanelManager.Instance != null)
            {
                UIPanelManager.Instance.ShowPanel("Confirm Popup", confirmData);
            }
        }
        
        #endregion
        
        #region Settings Persistence
        
        // OPT-48: Simplified — UpdateValues() already writes keys to PlayerPrefs.
        // We just need to flush to disk once on panel close.
        private void SaveSettings()
        {
            PlayerPrefs.Save();
        }
        
        #endregion
        
        #region Audio Helpers
        
        private void PlayButtonSound()
        {
            if (AudioManager.Instance != null && FMODEvents.Instance != null)
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnButton);
            }
        }
        
        #endregion
    }
}
