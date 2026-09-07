using System;
using UnityEngine;
using System.Collections.Generic;
using Base.Character.Stats;

namespace Base.Character.Skills
{
    public class SkillManager : ISkillManager
    {
        private readonly Dictionary<SkillType, ISkill> _skills = new();
        private readonly IResource<int> _currentSkillPoint;

        public SkillManager()
        {
            // Initialize all skills with their max levels from configuration
            _skills[SkillType.Hand] = new CommonSkillImpl(SkillType.Hand, DefaultSettings.SkillMaxLevels[SkillType.Hand]);
            _skills[SkillType.Tongue] = new CommonSkillImpl(SkillType.Tongue, DefaultSettings.SkillMaxLevels[SkillType.Tongue]);
            _skills[SkillType.F] = new CommonSkillImpl(SkillType.F, DefaultSettings.SkillMaxLevels[SkillType.F]);
            _skills[SkillType.A] = new CommonSkillImpl(SkillType.A, DefaultSettings.SkillMaxLevels[SkillType.A]);
            _skills[SkillType.Cum] = new CommonSkillImpl(SkillType.Cum, DefaultSettings.SkillMaxLevels[SkillType.Cum]);
            _skills[SkillType.Bullet] = new CommonSkillImpl(SkillType.Bullet, DefaultSettings.SkillMaxLevels[SkillType.Bullet]);
            _skills[SkillType.LongNight] = new CommonSkillImpl(SkillType.LongNight, DefaultSettings.SkillMaxLevels[SkillType.LongNight]);
            _skills[SkillType.Size] = new CommonSkillImpl(SkillType.Size, DefaultSettings.SkillMaxLevels[SkillType.Size]);

            _currentSkillPoint = new CommonResource(BasicResource.SkillPoint, DefaultSettings.DefaultSkillPoint);
        }

        public UIAction UpdateSkill(SkillType skillType)
        {
            if (!_skills.TryGetValue(skillType, out var skill)) return new UIAction.FalseWithNothing();
            if (!skill.IsUnlocked()) return new UIAction.FalseWithNothing();
            
            var requiredPoints = skill.SkillPointRequired();
            if (requiredPoints < 0) return new UIAction.FalseWithNothing(); // Max level reached
            if (_currentSkillPoint.GetValues() < requiredPoints) return new UIAction.FalseWithNothing();
            if (!skill.Upgrade()) return new UIAction.FalseWithNothing();

            _currentSkillPoint.UpdateValues(_currentSkillPoint.GetValues() - requiredPoints);
            
            return new UIAction.UpdateNewValue<SkillType>(skillType, _currentSkillPoint.GetValues());
        }

        public UIAction DowngradeSkill(SkillType skillType)
        {
            if (!_skills.TryGetValue(skillType, out var skill) || !skill.IsUnlocked() || skill.SkillLevel() <= DefaultSettings.BeginLevel || !skill.Downgrade())
            {
                return new UIAction.FalseWithNothing();
            }

            // Refund the points for the previous level
            var refundPoints = skill.SkillPointRequired();
            if (refundPoints > 0)
            {
                _currentSkillPoint.UpdateValues(_currentSkillPoint.GetValues() + refundPoints);
            }

            return new UIAction.UpdateNewValue<SkillType>(skillType, _currentSkillPoint.GetValues());
        }

        public SkillEffect UseSkill(SkillType skillType)
        {
            if (!_skills.TryGetValue(skillType, out var skill)) throw new Exception("No such skill");

            IncreasePoint(skillType);
            var effect = skill.Used();
            
            // For F and A skills, apply Size skill effect (stamina consumption)
            if ((skill.SkillType is SkillType.F or SkillType.A) && effect is SkillEffect.UseEnergy skillEffect)
            {
                _skills.TryGetValue(SkillType.Size, out var sizeSkill);
                effect = skillEffect with { EnergyTake = sizeSkill?.SkillValue() ?? 100 };
            }

            return effect;
        }

        public int GetSkillPoint()
        {
            return _currentSkillPoint.GetValues();
        }

        public ISkill GetSkill(SkillType skillType)
        {
            return _skills.TryGetValue(skillType, out var skill) ? skill : null;
        }

        public bool IsSkillUnlocked(SkillType skillType)
        {
            return _skills.TryGetValue(skillType, out var skill) && skill.IsUnlocked();
        }

        public bool SetSkillUnlocked(SkillType skillType, bool unlocked)
        {
            return _skills.TryGetValue(skillType, out var skill) && skill.SetUnlocked(unlocked);
        }

        public int GetStaminaLimitBonus()
        {
            return _skills.TryGetValue(SkillType.LongNight, out var longNightSkill) ? longNightSkill.SkillValue() : 0;
        }

        private void IncreasePoint(SkillType skillType)
        {
            if (!DefaultSettings.DefaultPointReceived.TryGetValue(skillType, out var increasePoint)) return;
            if (increasePoint <= 0) return;
            
            var newPoints = _currentSkillPoint.GetValues() + increasePoint;
            _currentSkillPoint.UpdateValues(newPoints);
        }

        /// <summary>
        /// Directly increase skill points by a specific amount
        /// Used for sex simulation rewards
        /// </summary>
        public void IncreaseSkillPointDirect(int amount)
        {
            if (amount <= 0) return;
            
            var newPoints = _currentSkillPoint.GetValues() + amount;
            _currentSkillPoint.UpdateValues(newPoints);
#if UNITY_EDITOR
            Debug.Log($"[SkillManager] Skill points increased by {amount} → {newPoints}");
#endif
        }

        /// <summary>
        /// Directly set skill points to an exact value (used by save/load restore).
        /// </summary>
        public void SetSkillPointDirect(int value)
        {
            _currentSkillPoint.UpdateValues(System.Math.Max(0, value));
        }

        public bool TrySpendSkillPoints(int amount, out int newValue)
        {
            newValue = _currentSkillPoint.GetValues();
            if (amount < 0)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            int current = _currentSkillPoint.GetValues();
            if (current < amount)
            {
                return false;
            }

            newValue = current - amount;
            _currentSkillPoint.UpdateValues(newValue);
            return true;
        }
    }
}
