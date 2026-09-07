using Base.Character;
using Base.Character.Action;
using Base.Character.Stats;
using Base.SexScenes;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class SexSceneUnlockServiceTests
{
    private readonly Dictionary<SexSceneType, int?> originalUnlockStates = new();
    private int? originalSkillPoints;

    [SetUp]
    public void SetUp()
    {
        foreach (SexSceneType sceneType in System.Enum.GetValues(typeof(SexSceneType)))
        {
            string key = $"SexSceneUnlocked_{sceneType}";
            originalUnlockStates[sceneType] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
            PlayerPrefs.DeleteKey($"SexSceneUnlocked_{sceneType}");
        }

        originalSkillPoints = PlayerPrefs.HasKey("BasicResource_SkillPoint")
            ? PlayerPrefs.GetInt("BasicResource_SkillPoint")
            : null;
        PlayerPrefs.DeleteKey("BasicResource_SkillPoint");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (KeyValuePair<SexSceneType, int?> pair in originalUnlockStates)
        {
            string key = $"SexSceneUnlocked_{pair.Key}";
            if (pair.Value.HasValue)
            {
                PlayerPrefs.SetInt(key, pair.Value.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        if (originalSkillPoints.HasValue)
        {
            PlayerPrefs.SetInt("BasicResource_SkillPoint", originalSkillPoints.Value);
        }
        else
        {
            PlayerPrefs.DeleteKey("BasicResource_SkillPoint");
        }
    }

    [Test]
    public void CostOnlySceneUnlocksWhenSexPointsAreSufficient()
    {
        Player player = CreatePlayerWithSkillPoints(50);
        Target mai = new Target("Mai");

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Blowjob, player, mai), Is.True);
    }

    [Test]
    public void CostOnlySceneFailsWhenSexPointsAreInsufficient()
    {
        Player player = CreatePlayerWithSkillPoints(49);
        Target mai = new Target("Mai");

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Blowjob, player, mai), Is.False);
    }

    [Test]
    public void DefaultScenesUseConfiguredUnlockState()
    {
        Assert.That(SexSceneUnlockService.IsUnlocked(SexSceneType.Missionary), Is.True);
        Assert.That(SexSceneUnlockService.IsUnlocked(SexSceneType.RoleplayPussy), Is.True);
        Assert.That(SexSceneUnlockService.IsUnlocked(SexSceneType.Blowjob), Is.False);
    }

    [Test]
    public void DoggyStyleRequiresBothSensitiveLevelsAtBoundary()
    {
        Player player = CreatePlayerWithSkillPoints(200);
        Target mai = new Target("Mai");
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.PussySensitiveLevel, 2)).Wait();
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.ButtholeSensitiveLevel, 2)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.DoggyStyle, player, mai), Is.True);

        Target failingMai = new Target("Mai");
        failingMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.PussySensitiveLevel, 2)).Wait();
        failingMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.ButtholeSensitiveLevel, 1)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.DoggyStyle, player, failingMai), Is.False);
    }

    [Test]
    public void CowgirlRequiresPussySensitiveLevelAndLewdLevelAtBoundary()
    {
        Player player = CreatePlayerWithSkillPoints(300);
        Target mai = new Target("Mai");
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.PussySensitiveLevel, 4)).Wait();
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.LewdLevel, 1)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Cowgirl, player, mai), Is.True);

        Target failingMai = new Target("Mai");
        failingMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.PussySensitiveLevel, 4)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Cowgirl, player, failingMai), Is.False);
    }

    [Test]
    public void SpooningRequiresStrictLoveAndLewdLevel()
    {
        Player player = CreatePlayerWithSkillPoints(500);
        Target boundaryMai = new Target("Mai");
        boundaryMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.Love, 200)).Wait();
        boundaryMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.LewdLevel, 1)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Spooning, player, boundaryMai), Is.False);

        Target passingMai = new Target("Mai");
        passingMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.Love, 201)).Wait();
        passingMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.LewdLevel, 1)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Spooning, player, passingMai), Is.True);
    }

    [Test]
    public void MatingPressRequiresBothSensitiveLevelsAndLewdLevelAtBoundary()
    {
        Player player = CreatePlayerWithSkillPoints(700);
        Target mai = new Target("Mai");
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.PussySensitiveLevel, 5)).Wait();
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.ButtholeSensitiveLevel, 5)).Wait();
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.LewdLevel, 2)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.MatingPress, player, mai), Is.True);

        Target failingMai = new Target("Mai");
        failingMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.PussySensitiveLevel, 5)).Wait();
        failingMai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.LewdLevel, 2)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.MatingPress, player, failingMai), Is.False);
    }

    [Test]
    public void StrictGreaterThanRequirementsDoNotPassAtEqualBoundary()
    {
        Player player = CreatePlayerWithSkillPoints(1000);
        player.DoAction(new CharacterActions.ChangeMaxStat(RewardTarget.Player, BasicStats.Stamina, 200)).Wait();
        Target mai = new Target("Mai");
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.Love, 400)).Wait();
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.LewdLevel, 3)).Wait();

        Assert.That(player.GetMaxStamina(), Is.EqualTo(300));
        Assert.That(mai.GetLove(), Is.EqualTo(400));
        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.FullNelson, player, mai), Is.False);
    }

    [Test]
    public void FullNelsonPassesWhenStrictRequirementsAreExceeded()
    {
        Player player = CreatePlayerWithSkillPoints(1000);
        player.DoAction(new CharacterActions.ChangeMaxStat(RewardTarget.Player, BasicStats.Stamina, 201)).Wait();
        Target mai = new Target("Mai");
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.Love, 401)).Wait();
        mai.DoAction(new CharacterActions.ChangeStat(RewardTarget.Mai, BasicStats.LewdLevel, 3)).Wait();

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.FullNelson, player, mai), Is.True);
    }

    [Test]
    public void SuccessfulUnlockSpendsExactCostAndPersists()
    {
        Player player = CreatePlayerWithSkillPoints(50);
        Target mai = new Target("Mai");

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Blowjob, player, mai), Is.True);
        Assert.That(player.TrySpendSkillPoints(50), Is.True);
        SexSceneUnlockService.SetUnlocked(SexSceneType.Blowjob, true);

        Assert.That(player.GetSkillPoint(), Is.EqualTo(0));
        Assert.That(SexSceneUnlockService.IsUnlocked(SexSceneType.Blowjob), Is.True);
    }

    [Test]
    public void FailedUnlockDoesNotSpendOrPersist()
    {
        Player player = CreatePlayerWithSkillPoints(300);
        Target mai = new Target("Mai");

        Assert.That(SexSceneUnlockService.CanUnlock(SexSceneType.Cowgirl, player, mai), Is.False);
        Assert.That(player.GetSkillPoint(), Is.EqualTo(300));
        Assert.That(SexSceneUnlockService.IsUnlocked(SexSceneType.Cowgirl), Is.False);
    }

    private static Player CreatePlayerWithSkillPoints(int skillPoints)
    {
        Player player = new Player("Player");
        player.DoAction(new CharacterActions.ChangeResource(
            RewardTarget.Player,
            BasicResource.SkillPoint,
            skillPoints)).Wait();
        return player;
    }
}
