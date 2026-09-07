using System.Collections.Generic;
using Base.Character;
using Base.Character.Stats;

namespace Base.SexScenes
{
    public static class SexSceneUnlockService
    {
        private const string UnlockKeyPrefix = "SexSceneUnlocked_";

        private static readonly IReadOnlyDictionary<SexSceneType, SexSceneDefinition> Definitions =
            new Dictionary<SexSceneType, SexSceneDefinition>
            {
                [SexSceneType.Missionary] = new SexSceneDefinition(
                    SexSceneType.Missionary,
                    "Missionary",
                    null,
                    "Sex Scene Unlock Requirements Missionary",
                    0,
                    defaultUnlocked: true),
                [SexSceneType.RoleplayPussy] = new SexSceneDefinition(
                    SexSceneType.RoleplayPussy,
                    "Roleplay Pussy",
                    "Foreplay Pussy",
                    "Sex Scene Unlock Requirements Roleplay Pussy",
                    0,
                    defaultUnlocked: true),
                [SexSceneType.RoleplayButthole] = new SexSceneDefinition(
                    SexSceneType.RoleplayButthole,
                    "Roleplay Butthole",
                    "Foreplay Butthole",
                    "Sex Scene Unlock Requirements Roleplay Butthole",
                    50,
                    defaultUnlocked: false),
                [SexSceneType.Blowjob] = new SexSceneDefinition(
                    SexSceneType.Blowjob,
                    "Blowjob",
                    "Blowjob",
                    "Sex Scene Unlock Requirements Blowjob",
                    50,
                    defaultUnlocked: false),
                [SexSceneType.Paizuri] = new SexSceneDefinition(
                    SexSceneType.Paizuri,
                    "Paizuri",
                    "Paizuri",
                    "Sex Scene Unlock Requirements Paizuri",
                    50,
                    defaultUnlocked: false),
                [SexSceneType.DoggyStyle] = new SexSceneDefinition(
                    SexSceneType.DoggyStyle,
                    "Doggy Style",
                    "Doggy Style",
                    "Sex Scene Unlock Requirements Doggy Style",
                    200,
                    defaultUnlocked: false,
                    new SexSceneRequirement(SexSceneRequirementType.SensitiveLevel, 3, SensitiveBodyPart.Pussy),
                    new SexSceneRequirement(SexSceneRequirementType.SensitiveLevel, 3, SensitiveBodyPart.Butthole)),
                [SexSceneType.Cowgirl] = new SexSceneDefinition(
                    SexSceneType.Cowgirl,
                    "Cowgirl",
                    "Cowgirl",
                    "Sex Scene Unlock Requirements Cowgirl",
                    300,
                    defaultUnlocked: false,
                    new SexSceneRequirement(SexSceneRequirementType.SensitiveLevel, 5, SensitiveBodyPart.Pussy),
                    new SexSceneRequirement(SexSceneRequirementType.LewdLevel, 2)),
                [SexSceneType.Spooning] = new SexSceneDefinition(
                    SexSceneType.Spooning,
                    "Spooning",
                    "Spooning",
                    "Sex Scene Unlock Requirements Spooning",
                    500,
                    defaultUnlocked: false,
                    new SexSceneRequirement(SexSceneRequirementType.LoveStrictGreaterThan, 200),
                    new SexSceneRequirement(SexSceneRequirementType.LewdLevel, 2)),
                [SexSceneType.MatingPress] = new SexSceneDefinition(
                    SexSceneType.MatingPress,
                    "Mating Press",
                    "Mating Press",
                    "Sex Scene Unlock Requirements Mating Press",
                    700,
                    defaultUnlocked: false,
                    new SexSceneRequirement(SexSceneRequirementType.SensitiveLevel, 6, SensitiveBodyPart.Pussy),
                    new SexSceneRequirement(SexSceneRequirementType.SensitiveLevel, 6, SensitiveBodyPart.Butthole),
                    new SexSceneRequirement(SexSceneRequirementType.LewdLevel, 3)),
                [SexSceneType.FullNelson] = new SexSceneDefinition(
                    SexSceneType.FullNelson,
                    "Full Nelson",
                    "Full Nelson",
                    "Sex Scene Unlock Requirements Full Nelson",
                    1000,
                    defaultUnlocked: false,
                    new SexSceneRequirement(SexSceneRequirementType.MaxStaminaStrictGreaterThan, 300),
                    new SexSceneRequirement(SexSceneRequirementType.LoveStrictGreaterThan, 400),
                    new SexSceneRequirement(SexSceneRequirementType.LewdLevel, 4))
            };

        public static IReadOnlyDictionary<SexSceneType, SexSceneDefinition> GetDefinitions()
        {
            return Definitions;
        }

        public static SexSceneDefinition GetDefinition(SexSceneType sceneType)
        {
            return Definitions[sceneType];
        }

        public static bool IsUnlocked(SexSceneType sceneType)
        {
            SexSceneDefinition definition = GetDefinition(sceneType);
            return UnityEngine.PlayerPrefs.GetInt(GetUnlockKey(sceneType), definition.DefaultUnlocked ? 1 : 0) == 1;
        }

        public static void SetUnlocked(SexSceneType sceneType, bool unlocked)
        {
            UnityEngine.PlayerPrefs.SetInt(GetUnlockKey(sceneType), unlocked ? 1 : 0);
        }

        public static bool CanUnlock(SexSceneType sceneType, Player player, Target mai)
        {
            if (player == null || mai == null)
            {
                return false;
            }

            if (IsUnlocked(sceneType))
            {
                return true;
            }

            SexSceneDefinition definition = GetDefinition(sceneType);
            if (player.GetSkillPoint() < definition.Cost)
            {
                return false;
            }

            foreach (SexSceneRequirement requirement in definition.Requirements)
            {
                if (!requirement.IsMet(player, mai))
                {
                    return false;
                }
            }

            return true;
        }

        public static string GetRequirementText(SexSceneType sceneType)
        {
            return GetDefinition(sceneType).BuildRequirementText();
        }

        private static string GetUnlockKey(SexSceneType sceneType)
        {
            return $"{UnlockKeyPrefix}{sceneType}";
        }
    }
}
