using System;
using System.Threading.Tasks;
using Base.Character;
using Base.Character.Action;
using Base.Character.Skills;
using Base.Inventory.Item;
using UnityEngine;

namespace Base
{
    [CreateAssetMenu(menuName = "Mai's Love Story/Data Manager", fileName = "DataManager")]
    public class DataManager: ScriptableObject
    {
        private IPlayer _player;
        private readonly ITarget _currentBoss = new Target("Mai");

        public DataManager()
        {
        }

        public DataManager(string playerName)
        {
            OnCreateNewPlayer(playerName);
        }

        public void OnCreateNewPlayer(string playerName)
        {
            _player = new Player(playerName);
        }

        public IPlayer GetPlayer() => _player;

        public ITarget GetCurrentBoss() => _currentBoss;

        public Task<UIAction> IncreaseSkillLevel(SkillType skillType)
        {
            return _player.DoAction(new CharacterActions.SkillLevelUp(skillType));
        }

        public Task<UIAction> DowngradeSkill(SkillType skillType)
        {
            return _player.DoAction(new CharacterActions.DowngradeSkill(skillType));
        }

        public Task<UIAction> OnBuyItem(IItem item, Action<UIAction> onUpdateUI)
        {
            return _player.DoAction(new CharacterActions.BuyItem(item));
        }

        public async Task OnUseItem(IItem item, Action<UIAction> onUpdateUI)
        {
            var action = await _player.DoAction(new CharacterActions.UseItem(item));
            onUpdateUI?.Invoke(action);
            if (item.IsForGift())
            {
                action = await _currentBoss.DoAction(new CharacterActions.ReceiveItem(item));
                onUpdateUI?.Invoke(action);
            }
        }

        public async Task UseSkillOnBoss(SkillType skillType, Action<UIAction> onUpdateUI)
        {
            var action = await _player.DoAction(new CharacterActions.UseSkill(skillType));
            switch (action)
            {
                case UIAction.UpdateNewValue<SkillEffect> effect:
                    var uiAction = await _currentBoss.DoAction(new CharacterActions.ReceiveEffect(effect.Type));
                    onUpdateUI?.Invoke(uiAction);
                    break;
            }
        }
    }
}