using System;
using UnityEngine;

namespace Base.Character.Skills
{
    public class CommonSkillImpl : ISkill
    {
        public SkillType SkillType { get; }

        private int MaxSkillLevel { get; }
        private int _currentValue;
        private int _currentLevel;
        private bool _isUnlocked;

        // OPT-38: Cached PlayerPrefs key strings to avoid string interpolation per Upgrade/Downgrade
        private readonly string _valueKey;
        private readonly string _levelKey;
        private readonly string _unlockKey;

        public CommonSkillImpl(SkillType skillType, int maxSkillLevel)
        {
            SkillType = skillType;
            MaxSkillLevel = maxSkillLevel;
            _valueKey = $"SkillValue_{skillType}";
            _levelKey = $"SkillLevel_{skillType}";
            _unlockKey = $"SkillUnlocked_{skillType}";
            _currentValue = PlayerPrefs.GetInt(
                _valueKey,
                DefaultSettings.DefaultSkillValue[skillType]
            );
            _currentLevel = PlayerPrefs.GetInt(_levelKey, DefaultSettings.BeginLevel);
            bool defaultUnlocked = DefaultSettings.DefaultSkillUnlocked.TryGetValue(skillType, out bool unlocked) && unlocked;
            _isUnlocked = PlayerPrefs.GetInt(_unlockKey, defaultUnlocked ? 1 : 0) == 1;
        }

        public int SkillValue()
        {
            return _currentValue;
        }

        public int SkillLevel()
        {
            return _currentLevel;
        }

        public bool IsUnlocked()
        {
            return _isUnlocked;
        }

        public bool SetUnlocked(bool unlocked)
        {
            if (_isUnlocked == unlocked) return false;

            _isUnlocked = unlocked;
            PlayerPrefs.SetInt(_unlockKey, _isUnlocked ? 1 : 0);
            return true;
        }

        public bool Upgrade()
        {
            if (!_isUnlocked) return false;
            if (_currentLevel >= MaxSkillLevel) return false;
            
            _currentValue += DefaultSettings.DefaultSkillValueIncrease[SkillType];
            _currentLevel++;
            PlayerPrefs.SetInt(_valueKey, _currentValue);
            PlayerPrefs.SetInt(_levelKey, _currentLevel);
            return true;
        }

        public bool Downgrade()
        {
            if (!_isUnlocked) return false;
            if (_currentLevel <= DefaultSettings.BeginLevel) return false;
            
            _currentValue -= DefaultSettings.DefaultSkillValueIncrease[SkillType];
            _currentLevel--;
            PlayerPrefs.SetInt(_valueKey, _currentValue);
            PlayerPrefs.SetInt(_levelKey, _currentLevel);
            return true;
        }

        /// <summary>
        /// Reset to default values and clear PlayerPrefs. Called on new game start.
        /// </summary>
        public void ResetToDefault()
        {
            _currentValue = DefaultSettings.DefaultSkillValue[SkillType];
            _currentLevel = DefaultSettings.BeginLevel;
            _isUnlocked = DefaultSettings.DefaultSkillUnlocked.TryGetValue(SkillType, out bool unlocked) && unlocked;
            PlayerPrefs.DeleteKey(_valueKey);
            PlayerPrefs.DeleteKey(_levelKey);
            PlayerPrefs.DeleteKey(_unlockKey);
        }

        public int SkillPointRequired()
        {
            var curLevel = SkillLevel();
            if (!DefaultSettings.SkillUpgradeRequirements.TryGetValue(SkillType, out var requirements))
                return -1;
            if (!requirements.TryGetValue(curLevel, out var required))
                return -1;
            return required; // Returns -1 if at max level
        }

        public SkillEffect Used()
        {
            return SkillType switch
            {
                SkillType.Hand or SkillType.Tongue =>
                    new SkillEffect.OrgasmIncrease(_currentValue),
                SkillType.F or SkillType.A =>
                    new SkillEffect.UseEnergy(_currentValue, 0),
                SkillType.Cum => new SkillEffect.CumIncreaseProcess(_currentValue),
                SkillType.Bullet => new SkillEffect.BulletCount(_currentValue),
                SkillType.LongNight => new SkillEffect.StaminaLimitBonus(_currentValue),
                SkillType.Size => new SkillEffect.UseEnergy(0, _currentValue),
                _ => throw new NotSupportedException("Skill type not supported")
            };
        }
    }
}
