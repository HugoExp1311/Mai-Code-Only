using System.Collections.Generic;

using Base.Character.Action;
using Base.Character.Stats;
using Base.Dialogues;
using Base.Inventory.Item;
using EventBus;
using Events;
using System.Threading.Tasks;

namespace Base.Character
{
    public class Target : ITarget
    {
        public Target(string name)
        {
            _name = name;
        }

        private readonly string _name;

        private readonly IStats<int> _love = new CommonStat<int>(
            BasicStats.Love,
            DefaultSettings.DefaultLove
        );

        private readonly IStats<int> _libido = new CommonStat<int>(
            BasicStats.Libido,
            DefaultSettings.DefaultLibido
        );

        private readonly IStats<int> _boobsSensitivePoints = new CommonStat<int>(
            BasicStats.BoobsSensitivePoints,
            DefaultSettings.DefaultBoobsSensitivePoints
        );

        private readonly IStats<int> _mouthSensitivePoints = new CommonStat<int>(
            BasicStats.MouthSensitivePoints,
            DefaultSettings.DefaultMouthSensitivePoints
        );

        private readonly IStats<int> _pussySensitivePoints = new CommonStat<int>(
            BasicStats.PussySensitivePoints,
            DefaultSettings.DefaultPussySensitivePoints
        );

        private readonly IStats<int> _buttholeSensitivePoints = new CommonStat<int>(
            BasicStats.ButtholeSensitivePoints,
            DefaultSettings.DefaultButtholeSensitivePoints
        );

        private readonly IStats<int> _lewdLevel = new CommonStat<int>(
            BasicStats.LewdLevel,
            DefaultSettings.DefaultLewdLevel,
            DefaultSettings.MaxLewdLevel
        );

        private readonly IStats<int> _boobsSensitiveLevel = new CommonStat<int>(
            BasicStats.BoobsSensitiveLevel,
            DefaultSettings.DefaultSensitiveLevel,
            DefaultSettings.MaxSensitiveLevel
        );

        private readonly IStats<int> _mouthSensitiveLevel = new CommonStat<int>(
            BasicStats.MouthSensitiveLevel,
            DefaultSettings.DefaultSensitiveLevel,
            DefaultSettings.MaxSensitiveLevel
        );

        private readonly IStats<int> _pussySensitiveLevel = new CommonStat<int>(
            BasicStats.PussySensitiveLevel,
            DefaultSettings.DefaultSensitiveLevel,
            DefaultSettings.MaxSensitiveLevel
        );

        private readonly IStats<int> _buttholeSensitiveLevel = new CommonStat<int>(
            BasicStats.ButtholeSensitiveLevel,
            DefaultSettings.DefaultSensitiveLevel,
            DefaultSettings.MaxSensitiveLevel
        );

        private readonly IStats<int> _pregnancyChance = new CommonStat<int>(
            BasicStats.PregnancyChance,
            DefaultSettings.DefaultPregnancyChance,
            DefaultSettings.MaxPregnancyChance
        );

        /**
         * return true if boss accept gift
         * else return false
         **/
        private UIAction ReceiveGift(IItem item)
        {
            // At the max Love Level, gift Love gains also default to +0.
            int giftLove = DefaultSettings.GetClampedLoveDelta(_love.GetValues(), item.Value);
            _love.UpdateValues(System.Math.Max(0, _love.GetValues() + giftLove));
            EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
            {
                Target = RewardTarget.Mai,
                Stat = BasicStats.Love,
                NewValue = _love.GetValues()
            });
            return new UIAction.DoneWithNothing();
        }

        private UIAction ReceiveEffect()
        {
            return new UIAction.DoneWithNothing();
        }

        private UIAction ChangeStat(CharacterActions.ChangeStat action)
        {
            if (action.Target != RewardTarget.Mai)
                return new UIAction.FalseWithNothing();

            switch (action.Stat)
            {
                case BasicStats.Love:
                    // At the max Love Level, rewards and penalties both default to +0.
                    int loveDelta = DefaultSettings.GetClampedLoveDelta(_love.GetValues(), action.Amount);
                    SetStatValue(_love, BasicStats.Love, System.Math.Max(0, _love.GetValues() + loveDelta));
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.Love, _love.GetValues());
                case BasicStats.Libido:
                    int libido = System.Math.Max(0, System.Math.Min(DefaultSettings.MaxLibido, _libido.GetValues() + action.Amount));
                    SetStatValue(_libido, BasicStats.Libido, libido, DefaultSettings.MaxLibido);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.Libido, _libido.GetValues());
                case BasicStats.BoobsSensitivePoints:
                    ChangeSensitivePoints(SensitiveBodyPart.Boobs, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.BoobsSensitivePoints, _boobsSensitivePoints.GetValues());
                case BasicStats.MouthSensitivePoints:
                    ChangeSensitivePoints(SensitiveBodyPart.Mouth, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.MouthSensitivePoints, _mouthSensitivePoints.GetValues());
                case BasicStats.PussySensitivePoints:
                    ChangeSensitivePoints(SensitiveBodyPart.Pussy, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.PussySensitivePoints, _pussySensitivePoints.GetValues());
                case BasicStats.ButtholeSensitivePoints:
                    ChangeSensitivePoints(SensitiveBodyPart.Butthole, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.ButtholeSensitivePoints, _buttholeSensitivePoints.GetValues());
                case BasicStats.LewdLevel:
                    int lewdLevel = System.Math.Max(
                        DefaultSettings.DefaultLewdLevel,
                        System.Math.Min(DefaultSettings.MaxLewdLevel, _lewdLevel.GetValues() + action.Amount));
                    SetStatValue(_lewdLevel, BasicStats.LewdLevel, lewdLevel, DefaultSettings.MaxLewdLevel);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.LewdLevel, _lewdLevel.GetValues());
                case BasicStats.BoobsSensitiveLevel:
                    ChangeSensitiveLevel(SensitiveBodyPart.Boobs, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.BoobsSensitiveLevel, GetSensitiveLevel(SensitiveBodyPart.Boobs));
                case BasicStats.MouthSensitiveLevel:
                    ChangeSensitiveLevel(SensitiveBodyPart.Mouth, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.MouthSensitiveLevel, GetSensitiveLevel(SensitiveBodyPart.Mouth));
                case BasicStats.PussySensitiveLevel:
                    ChangeSensitiveLevel(SensitiveBodyPart.Pussy, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.PussySensitiveLevel, GetSensitiveLevel(SensitiveBodyPart.Pussy));
                case BasicStats.ButtholeSensitiveLevel:
                    ChangeSensitiveLevel(SensitiveBodyPart.Butthole, action.Amount);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.ButtholeSensitiveLevel, GetSensitiveLevel(SensitiveBodyPart.Butthole));
                case BasicStats.PregnancyChance:
                    int pregnancyChance = System.Math.Max(
                        0,
                        System.Math.Min(DefaultSettings.MaxPregnancyChance, _pregnancyChance.GetValues() + action.Amount));
                    SetStatValue(_pregnancyChance, BasicStats.PregnancyChance, pregnancyChance, DefaultSettings.MaxPregnancyChance);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.PregnancyChance, _pregnancyChance.GetValues());
            }

            return new UIAction.DoneWithNothing();
        }

        private static void RaiseStatsChanged(BasicStats stat, int value, int maxValue = 0)
        {
            EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
            {
                Target = RewardTarget.Mai,
                Stat = stat,
                NewValue = value,
                MaxValue = maxValue
            });
        }

        private static void SetStatValue(IStats<int> stat, BasicStats basicStat, int value, int maxValue = 0)
        {
            stat.UpdateValues(value);
            RaiseStatsChanged(basicStat, value, maxValue);
        }

        private IStats<int> GetSensitivePointsStat(SensitiveBodyPart bodyPart)
        {
            return bodyPart switch
            {
                SensitiveBodyPart.Boobs => _boobsSensitivePoints,
                SensitiveBodyPart.Mouth => _mouthSensitivePoints,
                SensitiveBodyPart.Pussy => _pussySensitivePoints,
                SensitiveBodyPart.Butthole => _buttholeSensitivePoints,
                _ => _boobsSensitivePoints
            };
        }

        private IStats<int> GetSensitiveLevelStat(SensitiveBodyPart bodyPart)
        {
            return bodyPart switch
            {
                SensitiveBodyPart.Boobs => _boobsSensitiveLevel,
                SensitiveBodyPart.Mouth => _mouthSensitiveLevel,
                SensitiveBodyPart.Pussy => _pussySensitiveLevel,
                SensitiveBodyPart.Butthole => _buttholeSensitiveLevel,
                _ => _boobsSensitiveLevel
            };
        }

        private static BasicStats GetSensitivePointsBasicStat(SensitiveBodyPart bodyPart)
        {
            return bodyPart switch
            {
                SensitiveBodyPart.Boobs => BasicStats.BoobsSensitivePoints,
                SensitiveBodyPart.Mouth => BasicStats.MouthSensitivePoints,
                SensitiveBodyPart.Pussy => BasicStats.PussySensitivePoints,
                SensitiveBodyPart.Butthole => BasicStats.ButtholeSensitivePoints,
                _ => BasicStats.BoobsSensitivePoints
            };
        }

        private static BasicStats GetSensitiveLevelBasicStat(SensitiveBodyPart bodyPart)
        {
            return bodyPart switch
            {
                SensitiveBodyPart.Boobs => BasicStats.BoobsSensitiveLevel,
                SensitiveBodyPart.Mouth => BasicStats.MouthSensitiveLevel,
                SensitiveBodyPart.Pussy => BasicStats.PussySensitiveLevel,
                SensitiveBodyPart.Butthole => BasicStats.ButtholeSensitiveLevel,
                _ => BasicStats.BoobsSensitiveLevel
            };
        }

        private int ChangeSensitivePoints(SensitiveBodyPart bodyPart, int amount)
        {
            if (amount > 0 && IsMaxLewdLevel())
            {
                return GetSensitivePoints(bodyPart);
            }

            IStats<int> stat = GetSensitivePointsStat(bodyPart);
            int newValue = System.Math.Max(0, stat.GetValues() + amount);
            SetStatValue(stat, GetSensitivePointsBasicStat(bodyPart), newValue);
            return newValue;
        }

        private int ChangeSensitiveLevel(SensitiveBodyPart bodyPart, int amount)
        {
            IStats<int> stat = GetSensitiveLevelStat(bodyPart);
            int newValue = System.Math.Max(
                DefaultSettings.DefaultSensitiveLevel,
                System.Math.Min(DefaultSettings.MaxSensitiveLevel, stat.GetValues() + amount));
            SetStatValue(stat, GetSensitiveLevelBasicStat(bodyPart), newValue, DefaultSettings.MaxSensitiveLevel);
            return newValue;
        }

        private UIAction ChangeResource(CharacterActions.ChangeResource action)
        {
            if (action.Target != RewardTarget.Mai)
                return new UIAction.FalseWithNothing();

            // Mai doesn't have resources like Money, but keeping this for consistency
            // Can be extended in the future if needed
            return new UIAction.DoneWithNothing();
        }

        public Task<UIAction> DoAction(CharacterActions actions)
        {
            var result = actions switch
            {
                CharacterActions.ReceiveItem action => ReceiveGift(action.Item),
                CharacterActions.ReceiveEffect action => ReceiveEffect(),
                CharacterActions.ChangeStat action => ChangeStat(action),
                CharacterActions.ChangeResource action => ChangeResource(action),
                _ => new UIAction.DoneWithNothing()
            };
            return Task.FromResult(result);
        }

        public string GetName()
        {
            return _name;
        }

        public int GetLove()
        {
            return _love.GetValues();
        }

        /// <summary>
        /// Raw days-without-sex counter (0..MaxLibido). The named arousal state is derived
        /// from this via <see cref="GetLibidoState"/>.
        /// </summary>
        public int GetLibido()
        {
            return _libido.GetValues();
        }

        /// <summary>
        /// Derived Libido state index (0 = Not interested .. 4 = Craving for cock).
        /// </summary>
        public int GetLibidoState()
        {
            return DefaultSettings.GetLibidoState(_libido.GetValues());
        }

        /// <summary>
        /// True when Mai is willing to have sex (state at or above "Ready to have sex").
        /// </summary>
        public bool IsLibidoReadyForSex()
        {
            return DefaultSettings.IsLibidoReadyForSex(_libido.GetValues());
        }

        /// <summary>
        /// Cumulative Love Points (uncapped). See <see cref="GetLoveLevel"/> for the derived level.
        /// </summary>
        public int GetLoveLevel()
        {
            return DefaultSettings.GetLoveLevel(_love.GetValues());
        }

        public int GetBoobsSensitivePoints()
        {
            return _boobsSensitivePoints.GetValues();
        }

        public int GetMouthSensitivePoints()
        {
            return _mouthSensitivePoints.GetValues();
        }

        public int GetPussySensitivePoints()
        {
            return _pussySensitivePoints.GetValues();
        }

        public int GetButtholeSensitivePoints()
        {
            return _buttholeSensitivePoints.GetValues();
        }

        public int GetSensitivePoints(SensitiveBodyPart bodyPart)
        {
            return GetSensitivePointsStat(bodyPart).GetValues();
        }

        public int GetBoobsSensitiveLevel()
        {
            return GetSensitiveLevel(SensitiveBodyPart.Boobs);
        }

        public int GetMouthSensitiveLevel()
        {
            return GetSensitiveLevel(SensitiveBodyPart.Mouth);
        }

        public int GetPussySensitiveLevel()
        {
            return GetSensitiveLevel(SensitiveBodyPart.Pussy);
        }

        public int GetButtholeSensitiveLevel()
        {
            return GetSensitiveLevel(SensitiveBodyPart.Butthole);
        }

        public int GetSensitiveLevel(SensitiveBodyPart bodyPart)
        {
            return GetSensitiveLevelStat(bodyPart).GetValues();
        }

        public int GetSensitivePointsPerInteraction(SensitiveBodyPart bodyPart)
        {
            return DefaultSettings.GetSensitivePointsPerInteraction(GetSensitiveLevel(bodyPart));
        }

        /// <summary>
        /// Mai's current persistent Lewd Level. Upgrades deduct Sensitive Points.
        /// </summary>
        public int GetLewdLevel()
        {
            return _lewdLevel.GetValues();
        }

        public bool IsMaxLewdLevel()
        {
            return GetLewdLevel() >= DefaultSettings.MaxLewdLevel;
        }

        public bool CanUpgradeLewdLevel()
        {
            int targetLevel = GetLewdLevel() + 1;
            if (targetLevel > DefaultSettings.MaxLewdLevel)
                return false;

            if (!DefaultSettings.TryGetLewdLevelUpgradeRequirement(
                targetLevel,
                out int pussyButtholeCost,
                out int boobsMouthCost))
            {
                return false;
            }

            return _pussySensitivePoints.GetValues() >= pussyButtholeCost
                && _buttholeSensitivePoints.GetValues() >= pussyButtholeCost
                && _boobsSensitivePoints.GetValues() >= boobsMouthCost
                && _mouthSensitivePoints.GetValues() >= boobsMouthCost;
        }

        public bool TryUpgradeLewdLevel()
        {
            int targetLevel = GetLewdLevel() + 1;
            if (!CanUpgradeLewdLevel())
                return false;

            DefaultSettings.TryGetLewdLevelUpgradeRequirement(
                targetLevel,
                out int pussyButtholeCost,
                out int boobsMouthCost);

            ChangeSensitivePoints(SensitiveBodyPart.Pussy, -pussyButtholeCost);
            ChangeSensitivePoints(SensitiveBodyPart.Butthole, -pussyButtholeCost);
            ChangeSensitivePoints(SensitiveBodyPart.Boobs, -boobsMouthCost);
            ChangeSensitivePoints(SensitiveBodyPart.Mouth, -boobsMouthCost);
            SetStatValue(_lewdLevel, BasicStats.LewdLevel, targetLevel, DefaultSettings.MaxLewdLevel);
            return true;
        }

        public bool CanUpgradeSensitiveLevel(SensitiveBodyPart bodyPart)
        {
            int currentLevel = GetSensitiveLevel(bodyPart);
            int targetLevel = currentLevel + 1;
            if (targetLevel > DefaultSettings.MaxSensitiveLevel)
                return false;

            int cost = DefaultSettings.GetSensitiveLevelUpgradeRequirement(targetLevel);
            return cost >= 0 && GetSensitivePoints(bodyPart) >= cost;
        }

        public bool TryUpgradeSensitiveLevel(SensitiveBodyPart bodyPart)
        {
            int targetLevel = GetSensitiveLevel(bodyPart) + 1;
            int cost = DefaultSettings.GetSensitiveLevelUpgradeRequirement(targetLevel);
            if (cost < 0 || !CanUpgradeSensitiveLevel(bodyPart))
                return false;

            ChangeSensitivePoints(bodyPart, -cost);
            ChangeSensitiveLevel(bodyPart, 1);
            return true;
        }

        public bool AddSensitivePoints(SensitiveBodyPart bodyPart, int amount)
        {
            if (amount <= 0)
                return false;

            int before = GetSensitivePoints(bodyPart);
            int after = ChangeSensitivePoints(bodyPart, amount);
            return after != before;
        }

        public int GetPregnancyChance()
        {
            return _pregnancyChance.GetValues();
        }

        public void RecordCumInsideNoCondom(int count = 1)
        {
            if (count <= 0)
                return;

            int chanceIncrease = count * DefaultSettings.PregnancyChancePerCumInside;
            int newChance = System.Math.Min(DefaultSettings.MaxPregnancyChance, _pregnancyChance.GetValues() + chanceIncrease);
            SetStatValue(_pregnancyChance, BasicStats.PregnancyChance, newChance, DefaultSettings.MaxPregnancyChance);
        }

        public string GetPregnancyStatusText()
        {
            int chance = GetPregnancyChance();
            if (chance >= DefaultSettings.MaxPregnancyChance)
                return "Pregnant";

            return chance > 0 ? $"Not pregnant ({chance}%)" : "Not pregnant";
        }

        /// <summary>
        /// Apply sensitive points decay after a night without sex.
        /// Decreases all sensitive points by the configured decay amount, minimum 0.
        /// </summary>
        public void ApplySensitivePointsDecay()
        {
            int decay = DefaultSettings.SensitivePointsDecayPerNight;

            _boobsSensitivePoints.UpdateValues(System.Math.Max(0, _boobsSensitivePoints.GetValues() - decay));
            _mouthSensitivePoints.UpdateValues(System.Math.Max(0, _mouthSensitivePoints.GetValues() - decay));
            _pussySensitivePoints.UpdateValues(System.Math.Max(0, _pussySensitivePoints.GetValues() - decay));
            _buttholeSensitivePoints.UpdateValues(System.Math.Max(0, _buttholeSensitivePoints.GetValues() - decay));

            // PERF-3: Fire a single batched event instead of individual StatsChangedEvent raises
            EventBus<Events.SensitivePointsDecayEvent>.Raise(new Events.SensitivePointsDecayEvent());
        }

        /// <summary>
        /// Increment the days-without-sex counter by 1 (capped at MaxLibido) after a day
        /// without sex. Called by GameManager when advancing to the next day.
        /// </summary>
        public void IncrementLibido()
        {
            int currentLibido = _libido.GetValues();
            if (currentLibido < DefaultSettings.MaxLibido)
            {
                _libido.UpdateValues(currentLibido + 1);
                EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
                {
                    Target = RewardTarget.Mai,
                    Stat = BasicStats.Libido,
                    NewValue = _libido.GetValues()
                });
            }
        }

        /// <summary>
        /// Reset the days-without-sex counter to 0 after sex (Libido returns to "Not interested").
        /// Called by GameManager/SexSimulationManager after a sex scene.
        /// </summary>
        public void ResetLibido()
        {
            _libido.UpdateValues(0);
            EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
            {
                Target = RewardTarget.Mai,
                Stat = BasicStats.Libido,
                NewValue = 0
            });
        }

        /// <summary>Capture Mai's persistent relationship and body-stat state.</summary>
        public MaiState CaptureState()
        {
            return new MaiState
            {
                love = _love.GetValues(),
                libido = _libido.GetValues(),
                lewdLevel = _lewdLevel.GetValues(),
                pregnancyChance = _pregnancyChance.GetValues(),
                boobsSensitivePoints = _boobsSensitivePoints.GetValues(),
                mouthSensitivePoints = _mouthSensitivePoints.GetValues(),
                pussySensitivePoints = _pussySensitivePoints.GetValues(),
                buttholeSensitivePoints = _buttholeSensitivePoints.GetValues(),
                boobsSensitiveLevel = _boobsSensitiveLevel.GetValues(),
                mouthSensitiveLevel = _mouthSensitiveLevel.GetValues(),
                pussySensitiveLevel = _pussySensitiveLevel.GetValues(),
                buttholeSensitiveLevel = _buttholeSensitiveLevel.GetValues()
            };
        }

        /// <summary>Restore Mai's persistent relationship and body-stat state.</summary>
        public void RestoreState(MaiState state)
        {
            if (state == null) return;

            RestoreState(
                state.love, state.libido, state.lewdLevel,
                state.boobsSensitivePoints, state.mouthSensitivePoints, state.pussySensitivePoints, state.buttholeSensitivePoints,
                state.boobsSensitiveLevel, state.mouthSensitiveLevel, state.pussySensitiveLevel, state.buttholeSensitiveLevel,
                state.pregnancyChance);
        }

        [System.Serializable]
        public class MaiState
        {
            public int love;
            public int libido;
            public int lewdLevel;
            public int pregnancyChance;
            public int boobsSensitivePoints;
            public int mouthSensitivePoints;
            public int pussySensitivePoints;
            public int buttholeSensitivePoints;
            public int boobsSensitiveLevel;
            public int mouthSensitiveLevel;
            public int pussySensitiveLevel;
            public int buttholeSensitiveLevel;
        }

        /// <summary>
        /// Restore Mai's full state from a save slot (used by save/load).
        /// Raises a single SensitivePointsDecayEvent-style batch refresh so UI panels re-read values.
        /// </summary>
        public void RestoreState(
            int love,
            int libido,
            int lewdLevel,
            int boobsSensitivePoints, int mouthSensitivePoints, int pussySensitivePoints, int buttholeSensitivePoints,
            int boobsSensitiveLevel, int mouthSensitiveLevel, int pussySensitiveLevel, int buttholeSensitiveLevel,
            int pregnancyChance)
        {
            _love.UpdateValues(System.Math.Max(0, love));
            _libido.UpdateValues(System.Math.Clamp(libido, 0, DefaultSettings.MaxLibido));
            _lewdLevel.UpdateValues(System.Math.Clamp(lewdLevel, DefaultSettings.DefaultLewdLevel, DefaultSettings.MaxLewdLevel));

            _boobsSensitivePoints.UpdateValues(System.Math.Max(0, boobsSensitivePoints));
            _mouthSensitivePoints.UpdateValues(System.Math.Max(0, mouthSensitivePoints));
            _pussySensitivePoints.UpdateValues(System.Math.Max(0, pussySensitivePoints));
            _buttholeSensitivePoints.UpdateValues(System.Math.Max(0, buttholeSensitivePoints));

            _boobsSensitiveLevel.UpdateValues(System.Math.Clamp(boobsSensitiveLevel, DefaultSettings.DefaultSensitiveLevel, DefaultSettings.MaxSensitiveLevel));
            _mouthSensitiveLevel.UpdateValues(System.Math.Clamp(mouthSensitiveLevel, DefaultSettings.DefaultSensitiveLevel, DefaultSettings.MaxSensitiveLevel));
            _pussySensitiveLevel.UpdateValues(System.Math.Clamp(pussySensitiveLevel, DefaultSettings.DefaultSensitiveLevel, DefaultSettings.MaxSensitiveLevel));
            _buttholeSensitiveLevel.UpdateValues(System.Math.Clamp(buttholeSensitiveLevel, DefaultSettings.DefaultSensitiveLevel, DefaultSettings.MaxSensitiveLevel));

            _pregnancyChance.UpdateValues(System.Math.Clamp(pregnancyChance, 0, DefaultSettings.MaxPregnancyChance));

            // Batch UI refresh (same event used after sensitive-points decay)
            EventBus<Events.SensitivePointsDecayEvent>.Raise(new Events.SensitivePointsDecayEvent());
        }
    }
}
