using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using Base.Character.Action;
using Base.Character.Skills;
using Base.Character.Stats;
using Base.Dialogues;
using Base.Inventory;
using Base.Inventory.Item;
using EventBus;
using Events;

namespace Base.Character
{
    public class Player : IPlayer
    {
        private readonly string _playerName;

        private readonly CommonStat<int> _stamina = new CommonStat<int>(
            BasicStats.Stamina,
            DefaultSettings.DefaultStamina,
            DefaultSettings.DefaultStamina // Initial max stamina = default stamina (100)
        );

        private readonly IBasic<int> _charming = new CommonStat<int>(
            BasicStats.Charming,
            DefaultSettings.DefaultCharming
        );

        private readonly IBasic<int> _knowledge = new CommonStat<int>(
            BasicStats.Knowledge,
            DefaultSettings.DefaultKnowledge
        );

        private readonly IBasic<int> _money = new CommonResource(BasicResource.Money, DefaultSettings.InitialMoney);

        private readonly IInventory _inventory = new Inventory.Inventory();

        private readonly ISkillManager _skillManager = new SkillManager();

        public Player(string playerName)
        {
            _playerName = playerName;
        }

        private UIAction UseItem(IItem item)
        {
            var contains = _inventory.HasItem(item);
            if (!contains) return new UIAction.FalseWithNothing();
            //TODO: Base on which item's Identifier
            _inventory.RemoveItem(item);
            switch (item.ItemType)
            {
                case ItemType.Energy or ItemType.SpecialEnergy:
                    int newStamina = System.Math.Min(_stamina.GetValues() + item.Value, _stamina.GetMaxValue());
                    _stamina.UpdateValues(newStamina);
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.Stamina, _stamina.GetValues());
            }

            return new UIAction.DoneWithNothing();
        }

        private UIAction BuyItem(IItem item)
        {
            var canBuy = item.GetValues() <= _money.GetValues();
            if (!canBuy) return new UIAction.FalseWithNothing();
            _money.UpdateValues(_money.GetValues() - item.GetValues());
            _inventory.AddItem(item);
            EventBus<ResourceChangedEvent>.Raise(new ResourceChangedEvent
            {
                Target = RewardTarget.Player,
                Resource = BasicResource.Money,
                NewValue = _money.GetValues()
            });
            return new UIAction.UpdateNewValue<BasicResource>(BasicResource.Money, _money.GetValues());
        }

        private UIAction ChangeStat(CharacterActions.ChangeStat action)
        {
            if (action.Target != RewardTarget.Player)
                return new UIAction.FalseWithNothing();

            switch (action.Stat)
            {
                case BasicStats.Stamina:
                    int clampedStamina = System.Math.Max(0, System.Math.Min(_stamina.GetValues() + action.Amount, _stamina.GetMaxValue()));
                    _stamina.UpdateValues(clampedStamina);
                    EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
                    {
                        Target = RewardTarget.Player,
                        Stat = BasicStats.Stamina,
                        NewValue = _stamina.GetValues(),
                        MaxValue = _stamina.GetMaxValue()
                    });
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.Stamina, _stamina.GetValues());
                case BasicStats.Charming:
                    _charming.UpdateValues(System.Math.Max(0, _charming.GetValues() + action.Amount));
                    EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
                    {
                        Target = RewardTarget.Player,
                        Stat = BasicStats.Charming,
                        NewValue = _charming.GetValues(),
                        MaxValue = 0 // Charming has no max
                    });
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.Charming, _charming.GetValues());
                case BasicStats.Knowledge:
                    _knowledge.UpdateValues(System.Math.Max(0, _knowledge.GetValues() + action.Amount));
                    EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
                    {
                        Target = RewardTarget.Player,
                        Stat = BasicStats.Knowledge,
                        NewValue = _knowledge.GetValues(),
                        MaxValue = 0 // Knowledge has no max
                    });
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.Knowledge, _knowledge.GetValues());
            }

            return new UIAction.DoneWithNothing();
        }

        private UIAction ChangeResource(CharacterActions.ChangeResource action)
        {
            if (action.Target != RewardTarget.Player)
                return new UIAction.FalseWithNothing();

            switch (action.Resource)
            {
                case BasicResource.Money:
                    _money.UpdateValues(System.Math.Max(0, _money.GetValues() + action.Amount));
                    EventBus<ResourceChangedEvent>.Raise(new ResourceChangedEvent
                    {
                        Target = RewardTarget.Player,
                        Resource = BasicResource.Money,
                        NewValue = _money.GetValues()
                    });
                    return new UIAction.UpdateNewValue<BasicResource>(BasicResource.Money, _money.GetValues());
                
                case BasicResource.SkillPoint:
                    // Delegate to SkillManager to increase skill points
                    if (_skillManager != null && action.Amount != 0)
                    {
                        _skillManager.IncreaseSkillPointDirect(action.Amount);
                        int newPoints = _skillManager.GetSkillPoint();
                        
                        EventBus<ResourceChangedEvent>.Raise(new ResourceChangedEvent
                        {
                            Target = RewardTarget.Player,
                            Resource = BasicResource.SkillPoint,
                            NewValue = newPoints
                        });
                        return new UIAction.UpdateNewValue<BasicResource>(BasicResource.SkillPoint, newPoints);
                    }
                    break;
            }

            return new UIAction.DoneWithNothing();
        }

        private UIAction ChangeMaxStat(CharacterActions.ChangeMaxStat action)
        {
            if (action.Target != RewardTarget.Player)
                return new UIAction.FalseWithNothing();

            switch (action.Stat)
            {
                case BasicStats.Stamina:
                    int currentMax = _stamina.GetMaxValue();
                    int newMax = currentMax + action.Amount;
                    _stamina.UpdateMaxValue(newMax);
                    
                    // Raise event for max stamina change (UI can listen to this)
                    EventBus<StatsChangedEvent>.Raise(new StatsChangedEvent
                    {
                        Target = RewardTarget.Player,
                        Stat = BasicStats.Stamina,
                        NewValue = _stamina.GetValues(), // Current value stays the same
                        MaxValue = newMax // New max value
                    });
                    
                    return new UIAction.UpdateNewValue<BasicStats>(BasicStats.Stamina, _stamina.GetValues());
                
                // Other stats don't have max values yet, but can be added here
                default:
                    return new UIAction.DoneWithNothing();
            }
        }

        public string GetName()
        {
            return _playerName;
        }

        public int GetStamina()
        {
            return _stamina.GetValues();
        }

        public int GetMaxStamina()
        {
            return _stamina.GetMaxValue();
        }

        public int GetMoney()
        {
            return _money.GetValues();
        }

        public int GetCharming()
        {
            return _charming.GetValues();
        }

        public int GetKnowledge()
        {
            return _knowledge.GetValues();
        }

        public int GetSkillPoint()
        {
            return _skillManager.GetSkillPoint();
        }

        /// <summary>
        /// Restore full player state from a save slot (no clamping side-effects).
        /// </summary>
        public void RestoreState(int stamina, int maxStamina, int charming, int knowledge, int money, int skillPoint)
        {
            _stamina.UpdateMaxValue(System.Math.Max(1, maxStamina));
            _stamina.UpdateValues(System.Math.Clamp(stamina, 0, System.Math.Max(1, maxStamina)));
            _charming.UpdateValues(charming);
            _knowledge.UpdateValues(knowledge);
            _money.UpdateValues(money);
            _skillManager.SetSkillPointDirect(skillPoint);
        }

        public bool TrySpendSkillPoints(int amount)
        {
            if (!_skillManager.TrySpendSkillPoints(amount, out int newValue))
            {
                return false;
            }

            EventBus<ResourceChangedEvent>.Raise(new ResourceChangedEvent
            {
                Target = RewardTarget.Player,
                Resource = BasicResource.SkillPoint,
                NewValue = newValue
            });

            return true;
        }

        public ISkill GetSkill(SkillType skillType)
        {
            return _skillManager.GetSkill(skillType);
        }

        public bool IsSkillUnlocked(SkillType skillType)
        {
            return _skillManager.IsSkillUnlocked(skillType);
        }

        public bool SetSkillUnlocked(SkillType skillType, bool unlocked)
        {
            return _skillManager.SetSkillUnlocked(skillType, unlocked);
        }

        public bool UnlockSkill(SkillType skillType)
        {
            return SetSkillUnlocked(skillType, true);
        }

        public int GetStaminaLimitBonus()
        {
            return _skillManager.GetStaminaLimitBonus();
        }
        
        public System.Collections.Generic.List<IItem> GetInventoryItems()
        {
            return _inventory.GetAllItems();
        }

        /// <summary>
        /// Snapshot player state for the save system. Caller maps to SaveData fields.
        /// </summary>
        public PlayerState CaptureState()
        {
            var state = new PlayerState
            {
                stamina = _stamina.GetValues(),
                maxStamina = _stamina.GetMaxValue(),
                charming = _charming.GetValues(),
                knowledge = _knowledge.GetValues(),
                money = _money.GetValues(),
                skillPoint = _skillManager.GetSkillPoint()
            };

            foreach (var item in _inventory.GetAllItems())
            {
                if (item.Amount() > 0)
                {
                    state.inventory.Add(new InventoryStateEntry
                    {
                        id = item.Identifier(),
                        amount = item.Amount(),
                        price = item.GetValues(),
                        type = (int)item.ItemType,
                        value = item.Value
                    });
                }
            }

            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                var skill = _skillManager.GetSkill(skillType);
                if (skill != null)
                {
                    state.skills.Add(new SkillStateEntry
                    {
                        skillType = (int)skillType,
                        level = skill.SkillLevel(),
                        unlocked = skill.IsUnlocked()
                    });
                }
            }

            return state;
        }

        /// <summary>
        /// Restore player state from a save snapshot. Reconstructs inventory items
        /// and skill levels/unlock flags.
        /// </summary>
        public void RestoreState(PlayerState state)
        {
            if (state == null) return;

            _stamina.UpdateMaxValue(state.maxStamina);
            _stamina.UpdateValues(state.stamina);
            _charming.UpdateValues(state.charming);
            _knowledge.UpdateValues(state.knowledge);
            _money.UpdateValues(state.money);

            foreach (var entry in state.skills)
            {
                var skillType = (SkillType)entry.skillType;
                _skillManager.SetSkillUnlocked(skillType, entry.unlocked);
                // Restore the exact level; downgrade first if the current save is older.
                var skill = _skillManager.GetSkill(skillType);
                while (skill != null && skill.SkillLevel() > entry.level && skill.SkillLevel() > DefaultSettings.BeginLevel)
                {
                    if (!skill.Downgrade())
                        break;
                }
                while (skill != null && skill.SkillLevel() < entry.level && skill.Upgrade())
                {
                    // Upgrade one level at a time until restored
                }
            }

            // Restore skill points last so upgrade/downgrade refunds don't leak into the saved value.
            _skillManager.SetSkillPointDirect(state.skillPoint);

            // Replace inventory contents so repeated loads stay idempotent.
            var restoredItems = new List<IItem>();
            foreach (var entry in state.inventory)
            {
                if (entry == null || entry.amount <= 0)
                    continue;
                restoredItems.Add(new CommonItem(entry.price, entry.id, (ItemType)entry.type, entry.value, entry.amount));
            }
            _inventory.ReplaceItems(restoredItems);
        }

        [Serializable]
        public class PlayerState
        {
            public int stamina;
            public int maxStamina;
            public int charming;
            public int knowledge;
            public int money;
            public int skillPoint;
            public List<InventoryStateEntry> inventory = new List<InventoryStateEntry>();
            public List<SkillStateEntry> skills = new List<SkillStateEntry>();
        }

        [Serializable]
        public class InventoryStateEntry
        {
            public string id;
            public int amount;
            public int price;
            public int type;
            public int value;
        }

        [Serializable]
        public class SkillStateEntry
        {
            public int skillType;
            public int level;
            public bool unlocked;
        }

        /**
         * Based on [BasicStats.Charming] we will return which player's line
         */
        public string Talk()
        {
            return "";
        }

        private UIAction UseSkill(SkillType type)
        {
            var value = _skillManager.UseSkill(type);
            return new UIAction.UpdateNewValue<SkillEffect>(value, 0);
        }

        public Task<UIAction> DoAction(CharacterActions actions)
        {
            var result = actions switch
            {
                CharacterActions.UseItem action => UseItem(action.Item),
                CharacterActions.BuyItem action => BuyItem(action.Item),
                CharacterActions.SkillLevelUp action => _skillManager.UpdateSkill(action.SkillType),
                CharacterActions.DowngradeSkill action => _skillManager.DowngradeSkill(action.SkillType),
                CharacterActions.UseSkill action => UseSkill(action.SkillType),
                CharacterActions.ChangeStat action => ChangeStat(action),
                CharacterActions.ChangeMaxStat action => ChangeMaxStat(action),
                CharacterActions.ChangeResource action => ChangeResource(action),
                _ => new UIAction.FalseWithNothing()
            };
            return Task.FromResult(result);
        }
    }
}
