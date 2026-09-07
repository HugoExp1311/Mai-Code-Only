using System.Collections.Generic;
using System.Linq;
using Base.Character;
using Base.Character.Stats;

namespace Base.SexScenes
{
    public enum SexSceneType
    {
        Missionary,
        RoleplayPussy,
        RoleplayButthole,
        Blowjob,
        Paizuri,
        DoggyStyle,
        Cowgirl,
        Spooning,
        MatingPress,
        FullNelson
    }

    public enum SexSceneRequirementType
    {
        SensitiveLevel,
        LewdLevel,
        LoveStrictGreaterThan,
        MaxStaminaStrictGreaterThan
    }

    public readonly struct SexSceneRequirement
    {
        public SexSceneRequirement(
            SexSceneRequirementType requirementType,
            int requiredValue,
            SensitiveBodyPart? bodyPart = null)
        {
            RequirementType = requirementType;
            RequiredValue = requiredValue;
            BodyPart = bodyPart;
        }

        public SexSceneRequirementType RequirementType { get; }
        public int RequiredValue { get; }
        public SensitiveBodyPart? BodyPart { get; }

        public bool IsMet(Player player, Target mai)
        {
            if (player == null || mai == null)
            {
                return false;
            }

            return RequirementType switch
            {
                SexSceneRequirementType.SensitiveLevel when BodyPart.HasValue => mai.GetSensitiveLevel(BodyPart.Value) >= RequiredValue,
                SexSceneRequirementType.LewdLevel => mai.GetLewdLevel() >= RequiredValue,
                SexSceneRequirementType.LoveStrictGreaterThan => mai.GetLove() > RequiredValue,
                SexSceneRequirementType.MaxStaminaStrictGreaterThan => player.GetMaxStamina() > RequiredValue,
                _ => false
            };
        }

        public string ToDisplayText()
        {
            return RequirementType switch
            {
                SexSceneRequirementType.SensitiveLevel when BodyPart.HasValue =>
                    $"- Sensitive Level for {GetBodyPartLabel(BodyPart.Value)} must be {RequiredValue} or more.",
                SexSceneRequirementType.LewdLevel =>
                    $"- Lewd Level {RequiredValue} or more.",
                SexSceneRequirementType.LoveStrictGreaterThan =>
                    $"- Love Points must be over {RequiredValue} Points.",
                SexSceneRequirementType.MaxStaminaStrictGreaterThan =>
                    $"- Max Energy must be over {RequiredValue}.",
                _ => string.Empty
            };
        }

        private static string GetBodyPartLabel(SensitiveBodyPart bodyPart)
        {
            return bodyPart switch
            {
                SensitiveBodyPart.Boobs => "Boobs",
                SensitiveBodyPart.Mouth => "Mouth",
                SensitiveBodyPart.Pussy => "Pussy",
                SensitiveBodyPart.Butthole => "Butthole",
                _ => bodyPart.ToString()
            };
        }
    }

    public sealed class SexSceneDefinition
    {
        public SexSceneDefinition(
            SexSceneType sceneType,
            string skillSceneName,
            string simulationPositionName,
            string requirementLocalizationKey,
            int cost,
            bool defaultUnlocked,
            params SexSceneRequirement[] requirements)
        {
            SceneType = sceneType;
            SkillSceneName = skillSceneName;
            SimulationPositionName = simulationPositionName;
            RequirementLocalizationKey = requirementLocalizationKey;
            Cost = cost;
            DefaultUnlocked = defaultUnlocked;
            Requirements = requirements?.ToArray() ?? System.Array.Empty<SexSceneRequirement>();
        }

        public SexSceneType SceneType { get; }
        public string SkillSceneName { get; }
        public string SimulationPositionName { get; }
        public string RequirementLocalizationKey { get; }
        public int Cost { get; }
        public bool DefaultUnlocked { get; }
        public IReadOnlyList<SexSceneRequirement> Requirements { get; }

        public string BuildRequirementText()
        {
            List<string> lines = new List<string>
            {
                $"- Sex Points: {Cost}"
            };

            foreach (SexSceneRequirement requirement in Requirements)
            {
                string line = requirement.ToDisplayText();
                if (!string.IsNullOrEmpty(line))
                {
                    lines.Add(line);
                }
            }

            return string.Join("\n", lines);
        }
    }
}
