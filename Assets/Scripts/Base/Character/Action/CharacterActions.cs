using System;
using Base.Character.Skills;
using Base.Character.Stats;
using Base.Inventory.Item;

namespace Base.Character.Action
{
    public abstract record CharacterActions
    {
        /*Begin Player's actions*/
        public record BuyItem(IItem Item) : CharacterActions;

        public record UseItem(IItem Item) : CharacterActions;

        public record SkillLevelUp(SkillType SkillType) : CharacterActions;
        
        public record DowngradeSkill(SkillType SkillType) : CharacterActions;

        public record UseSkill(SkillType SkillType) : CharacterActions;
        
        public record Sleep(int Hours) : CharacterActions;
        
        public record Eating(int Hours) : CharacterActions;
        
        public record Sex(int Hours) : CharacterActions;

        /*End Player's actions*/

        /*Begin Boss's actions*/
        public record ReceiveItem(IItem Item) : CharacterActions;

        public record ReceiveEffect(SkillEffect Effect) : CharacterActions;
        /*End Boss's actions*/

        /*Shared actions*/
        public record ChangeStat(RewardTarget Target, BasicStats Stat, int Amount) : CharacterActions;

        public record ChangeMaxStat(RewardTarget Target, BasicStats Stat, int Amount) : CharacterActions;

        public record ChangeResource(RewardTarget Target, BasicResource Resource, int Amount) : CharacterActions;
        /*End Shared actions*/
    };
}