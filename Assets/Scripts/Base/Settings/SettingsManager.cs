using System;
using UnityEngine;

namespace Base.Settings
{
    public class SettingsManager
    {
        private readonly ISettings<int> _musicSetting = new Settings<int>(
            initialValue: DefaultSettings.DefaultMusicVolume,
            settingsType: SettingsType.Music
        );

        private readonly ISettings<int> _soundSettings = new Settings<int>(
            initialValue: DefaultSettings.DefaultSoundVolume,
            settingsType: SettingsType.Sound
        );

        private readonly ISettings<int> _voiceSettings = new Settings<int>(
            initialValue: DefaultSettings.DefaultVoiceVolume,
            settingsType: SettingsType.Voice
        );

        private readonly ISettings<Language> _languageSettings = new Settings<Language>(
            initialValue: DefaultSettings.DefaultLanguage,
            settingsType: SettingsType.Language
        );

        private readonly ISettings<ScreenSettings> _screenSettings = new Settings<ScreenSettings>(
            initialValue: DefaultSettings.DefaultScreenSettings,
            settingsType: SettingsType.Screen
        );

        public void UpdateSettings(SettingsType settingsType, dynamic value)
        {
            switch (settingsType)
            {
                case SettingsType.Music:
                    _musicSetting.UpdateValues(value);
                    break;
                case SettingsType.Sound:
                    _soundSettings.UpdateValues(value);
                    break;
                case SettingsType.Voice:
                    _voiceSettings.UpdateValues(value);
                    break;
                case SettingsType.Language:
                    _languageSettings.UpdateValues(value);
                    break;
                case SettingsType.Screen:
                    _screenSettings.UpdateValues(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public dynamic GetSetting(SettingsType settingsType)
        {
            switch (settingsType)
            {
                case SettingsType.Music:
                    return _musicSetting.GetValues();
                case SettingsType.Sound:
                    return _soundSettings.GetValues();
                case SettingsType.Voice:
                    return _voiceSettings.GetValues();
                case SettingsType.Language:
                    return _languageSettings.GetValues();
                case SettingsType.Screen:
                    return _screenSettings.GetValues();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Typed volume getters to avoid dynamic boxing overhead in hot paths (e.g., AudioManager.Update)
        public int GetMusicVolume() => _musicSetting.GetValues();
        public int GetSoundVolume() => _soundSettings.GetValues();
        public int GetVoiceVolume() => _voiceSettings.GetValues();
        
        // OPT-7: Typed getters for Language and Screen to avoid dynamic boxing
        public Language GetLanguage() => _languageSettings.GetValues();
        public ScreenSettings GetScreenSettings() => _screenSettings.GetValues();
        
        // OPT-8: Typed update methods to bypass dynamic parameter boxing
        public void UpdateMusicVolume(int volume) => _musicSetting.UpdateValues(volume);
        public void UpdateSoundVolume(int volume) => _soundSettings.UpdateValues(volume);
        public void UpdateVoiceVolume(int volume) => _voiceSettings.UpdateValues(volume);
        public void UpdateLanguage(Language lang) => _languageSettings.UpdateValues(lang);
        public void UpdateScreenSettings(ScreenSettings screen) => _screenSettings.UpdateValues(screen);
    }

    internal class Settings<T> : ISettings<T>
    {
        private T _currentValue;
        private readonly SettingsType _type;
        private readonly string _prefsKey; // OPT-50: Cached key to avoid ToString() per write

        public Settings(T initialValue, SettingsType settingsType)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(Language) && typeof(T) != typeof(ScreenSettings))
            {
                throw new NotImplementedException("T must be int, Language and ScreenSettings");
            }

            // OPT-50: Cache the key string once
            _prefsKey = settingsType.ToString();
            
            var savedValue = initialValue;
            if (typeof(T) == typeof(int))
            {
                savedValue = (T)(object)PlayerPrefs.GetInt(_prefsKey, (int)(object)initialValue);
            }
            else if (typeof(T) == typeof(Language))
            {
                int savedInt = PlayerPrefs.GetInt(_prefsKey, (int)(object)initialValue);
                savedValue = (T)(object)(Language)savedInt;
            }
            else if (typeof(T) == typeof(ScreenSettings))
            {
                int savedInt = PlayerPrefs.GetInt(_prefsKey, (int)(object)initialValue);
                savedValue = (T)(object)(ScreenSettings)savedInt;
            }

            _currentValue = savedValue;
            _type = settingsType;
        }

        public T GetValues()
        {
            return _currentValue;
        }

        public void UpdateValues(T value)
        {
            _currentValue = value;
            
            // Write to in-memory PlayerPrefs cache (uses cached key)
            // OPT-51: Do NOT call PlayerPrefs.Save() here — SettingPanel flushes on close
            PlayerPrefs.SetInt(_prefsKey, (int)(object)value);
        }

        public SettingsType GetSettingsType()
        {
            return _type;
        }
    }
}