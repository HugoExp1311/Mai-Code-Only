using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Base.SexScenes;
using Live2D;
using MaisLoveStory.Live2D;
using NUnit.Framework;
using UI.SexPosition;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoleplayButtholeContractTests
{
    private const string ControllerPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Butthole/Roleplay Butthole.controller";

    private const string PrefabPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Butthole/Roleplay Butthole.prefab";

    private const string ScenePath = "Assets/Scenes/Game.unity";

    private static readonly HashSet<string> NonLoopingClips = new()
    {
        "Roleplay Butthole Cumming 1",
        "Roleplay Butthole Toy - Egg (Insert)",
        "Roleplay Butthole Toy - Egg (Out 1)"
    };

    [Test]
    public void FactoryCreatesButtholeConfigWithoutChangingLegacyEnumValues()
    {
        Assert.AreEqual(0, (int)SexPositionConfigType.Missionary);
        Assert.AreEqual(1, (int)SexPositionConfigType.Doggy);
        Assert.AreEqual(2, (int)SexPositionConfigType.Cowgirl);
        Assert.AreEqual(3, (int)SexPositionConfigType.Standing);
        Assert.AreEqual(4, (int)SexPositionConfigType.RoleplayPussy);
        Assert.AreEqual(5, (int)SexPositionConfigType.RoleplayButthole);

        SexPositionConfig config =
            SexPositionConfigFactory.Create(SexPositionConfigType.RoleplayButthole);
        config.Initialize();

        Assert.IsInstanceOf<RoleplayButtholePositionConfig>(config);
        Assert.AreEqual("Roleplay Butthole", config.GetPositionName());
        Assert.AreEqual(SimulationState.Active, config.GetInitialState());
        Assert.AreEqual(4, config.GetRoleplayAnimationButtonCount());

        ButtonVisibilitySet active = config.GetButtonVisibility(SimulationState.Active);
        Assert.IsTrue(active.slowButton);
        Assert.IsTrue(active.fastButton);
        Assert.IsTrue(active.stopButton);
        Assert.IsTrue(active.roleplaySelectorButtons);
        Assert.IsTrue(active.finishButton);

        ButtonVisibilitySet afterCumming =
            config.GetButtonVisibility(SimulationState.AfterCumming);
        Assert.IsFalse(afterCumming.slowButton);
        Assert.IsFalse(afterCumming.fastButton);
        Assert.IsTrue(afterCumming.stopButton);
        Assert.IsFalse(afterCumming.roleplaySelectorButtons);
        Assert.IsTrue(afterCumming.finishButton);
    }

    [Test]
    public void CategoryButtonCountsAndBlendStartsMatchNumberedButtonContract()
    {
        var config = new RoleplayButtholePositionConfig();
        FieldInfo categoryField = typeof(RoleplayButtholePositionConfig).GetField(
            "currentCategory",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(categoryField);

        categoryField.SetValue(config, RoleplayCategory.Hand);
        Assert.AreEqual(4, config.GetRoleplayAnimationButtonCount());
        categoryField.SetValue(config, RoleplayCategory.Tongue);
        Assert.AreEqual(2, config.GetRoleplayAnimationButtonCount());
        categoryField.SetValue(config, RoleplayCategory.SexToy);
        Assert.AreEqual(3, config.GetRoleplayAnimationButtonCount());

        MethodInfo getFirstBlendIndex = typeof(RoleplayButtholePositionConfig).GetMethod(
            "GetFirstBlendIndex",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(getFirstBlendIndex);
        Assert.AreEqual(0, getFirstBlendIndex.Invoke(null, new object[] { RoleplayCategory.Hand }));
        Assert.AreEqual(2, getFirstBlendIndex.Invoke(null, new object[] { RoleplayCategory.Tongue }));
        Assert.AreEqual(4, getFirstBlendIndex.Invoke(null, new object[] { RoleplayCategory.SexToy }));
    }

    [Test]
    public void AnimationEventGateRejectsInactiveAndTransitioningPlayback()
    {
        MethodInfo isPlaybackEventAllowed =
            typeof(RoleplayButtholeAnimationEventReceiver).GetMethod(
                "IsPlaybackEventAllowed",
                BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(isPlaybackEventAllowed);

        Assert.IsFalse((bool)isPlaybackEventAllowed.Invoke(null, new object[] { false, false }));
        Assert.IsFalse((bool)isPlaybackEventAllowed.Invoke(null, new object[] { false, true }));
        Assert.IsFalse((bool)isPlaybackEventAllowed.Invoke(null, new object[] { true, true }));
        Assert.IsTrue((bool)isPlaybackEventAllowed.Invoke(null, new object[] { true, false }));
    }

    [Test]
    public void Live2DTestBenchRoutesButtholeCumToMaiAndBlowjobCumToPlayer()
    {
        const string testBenchPath =
            "Assets/Scripts/Live2D/Editor/Live2DTestBenchWindow.cs";
        MonoScript testBenchScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
            testBenchPath);

        Assert.NotNull(testBenchScript, $"Missing Test Bench script at {testBenchPath}");
        StringAssert.Contains(
            "? \"Player Cum 100%\"",
            testBenchScript.text);
        StringAssert.Contains(
            ": \"Mai Cum 100%\"",
            testBenchScript.text);
        StringAssert.Contains(
            "roleplayConfig.OnMaiOrgasm(roleplayPanel);",
            testBenchScript.text);
        StringAssert.Contains(
            "roleplayConfig.OnPlayerCumReached(roleplayPanel);",
            testBenchScript.text);
    }

    [Test]
    public void PlayerCumCapacityDoesNotRepresentMaiOrgasm()
    {
        var gameObject = new GameObject("Roleplay Butthole player capacity test");
        try
        {
            var panel = gameObject.AddComponent<SimulationNavigationPanel>();
            Button slowButton = CreateButton(gameObject, "Roleplay Slow");
            Button fastButton = CreateButton(gameObject, "Roleplay Fast");
            SetPrivateFieldValue(panel, "roleplaySlowButton", slowButton);
            SetPrivateFieldValue(panel, "roleplayFastButton", fastButton);

            var config = (RoleplayButtholePositionConfig)panel.ConfigureRoleplayTestBench(
                SexPositionConfigType.RoleplayButthole,
                null);

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Slow");

            config.OnPlayerCumReached(panel);

            AssertPrivateFieldString(config, "playbackMode", "Slow");
            Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);

            config.OnThrustStopped(panel);
            config.OnRoleplayStartEntered(panel);
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.SexToy);
            config.OnRoleplayAnimationSelected(panel, 2);
            config.OnRoleplayEggLoopEntered(panel);

            config.OnPlayerCumReached(panel);

            AssertPrivateFieldString(config, "playbackMode", "EggLoop");
            Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
            config.OnThrustStopped(panel);
            AssertPrivateFieldString(config, "playbackMode", "EggRemoving");
            Assert.AreEqual(SimulationState.Transitioning, panel.CurrentState);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void RuntimeSelectionStartsSlowAndKeepsActionsInterchangeableWithinCategory()
    {
        var gameObject = new GameObject("Roleplay Butthole runtime switch test");
        try
        {
            var panel = gameObject.AddComponent<SimulationNavigationPanel>();
            Button slowButton = CreateButton(gameObject, "Roleplay Slow");
            Button fastButton = CreateButton(gameObject, "Roleplay Fast");
            SetPrivateFieldValue(panel, "roleplaySlowButton", slowButton);
            SetPrivateFieldValue(panel, "roleplayFastButton", fastButton);

            var config = (RoleplayButtholePositionConfig)panel.ConfigureRoleplayTestBench(
                SexPositionConfigType.RoleplayButthole,
                null);

            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldString(config, "playbackMode", "Start");
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);

            config.OnRoleplayAnimationSelected(panel, 1);
            config.OnThrustStarted(panel, true);
            AssertPrivateFieldString(config, "playbackMode", "Start");

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Tongue);
            AssertPrivateFieldString(config, "currentCategory", "Hand");

            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 0);
            Assert.IsTrue(slowButton.interactable);
            Assert.IsTrue(fastButton.interactable);

            config.OnThrustStarted(panel, true);
            AssertPrivateFieldString(config, "playbackMode", "Fast");
            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 1);

            config.OnRoleplayAnimationSelected(panel, 3);
            AssertPrivateFieldString(config, "playbackMode", "FingerMassage");
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);
            config.OnThrustStarted(panel, true);
            AssertPrivateFieldString(config, "playbackMode", "FingerMassage");

            config.OnRoleplayAnimationSelected(panel, 4);
            AssertPrivateFieldString(config, "playbackMode", "HandGrabBoth");
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 0);

            config.OnThrustStopped(panel);
            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldString(config, "playbackMode", "ReturningToStart");
            config.OnRoleplayStartEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "Start");

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Tongue);
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 2);
            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 3);

            config.OnThrustStopped(panel);
            config.OnRoleplayStartEntered(panel);
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.SexToy);
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 4);
            config.OnRoleplayAnimationSelected(panel, 3);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 5);

            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "EggInsert");
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);
            config.OnRoleplayEggLoopEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "EggLoop");

            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "EggRemoving");
            AssertPrivateFieldString(config, "pendingPlaybackAction", "Slow");
            config.OnRoleplayStartEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 4);
            Assert.IsTrue(slowButton.interactable);
            Assert.IsTrue(fastButton.interactable);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void EggStopAndMaiCumRoutesEndInCumming2UntilExplicitStop()
    {
        var gameObject = new GameObject("Roleplay Butthole terminal route test");
        try
        {
            var panel = gameObject.AddComponent<SimulationNavigationPanel>();
            Button slowButton = CreateButton(gameObject, "Roleplay Slow");
            Button fastButton = CreateButton(gameObject, "Roleplay Fast");
            SetPrivateFieldValue(panel, "roleplaySlowButton", slowButton);
            SetPrivateFieldValue(panel, "roleplayFastButton", fastButton);

            var config = (RoleplayButtholePositionConfig)panel.ConfigureRoleplayTestBench(
                SexPositionConfigType.RoleplayButthole,
                null);

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.SexToy);
            config.OnRoleplayAnimationSelected(panel, 2);
            config.OnRoleplayEggLoopEntered(panel);
            config.OnThrustStopped(panel);

            AssertPrivateFieldString(config, "playbackMode", "EggRemoving");
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            config.OnAfterCummingEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "Cumming2");
            Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);

            config.OnThrustStopped(panel);
            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldString(config, "playbackMode", "ReturningToStart");
            config.OnRoleplayStartEntered(panel);

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.SexToy);
            config.OnRoleplayAnimationSelected(panel, 2);
            config.OnRoleplayEggLoopEntered(panel);
            config.OnMaiOrgasm(panel);
            AssertPrivateFieldString(config, "playbackMode", "EggRemoving");
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            config.OnAfterCummingEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "Cumming2");
            Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
            config.OnThrustStopped(panel);
            config.OnRoleplayStartEntered(panel);

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            config.OnRoleplayAnimationSelected(panel, 3);
            config.OnMaiOrgasm(panel);
            AssertPrivateFieldString(config, "playbackMode", "Cumming1");
            Assert.AreEqual(SimulationState.Transitioning, panel.CurrentState);
            config.OnAfterCummingEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "Cumming2");
            Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void OrdinaryLoopUsesSharedBarsAndCountsOnlyMaiOrgasm()
    {
        var gameObject = new GameObject("Roleplay Butthole ordinary bar test");
        FieldInfo instanceField = typeof(SexSimulationManager).GetField(
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousInstance = instanceField?.GetValue(null);

        try
        {
            instanceField?.SetValue(null, null);
            var manager = gameObject.AddComponent<SexSimulationManager>();
            manager.SetCurrentPosition(SexPositionConfigType.RoleplayButthole);
            manager.StartThrusting(Base.Character.Skills.SkillType.Hand, false);

            var player = new Base.Character.Player("Roleplay Butthole test player");
            object cumSkill = player.GetSkill(Base.Character.Skills.SkillType.Cum);
            object handSkill = player.GetSkill(Base.Character.Skills.SkillType.Hand);
            SetPrivateFieldValue(cumSkill, "_currentValue", 10);
            SetPrivateFieldValue(handSkill, "_currentValue", 10);
            SetPrivateFieldValue(manager, "_cachedPlayer", player);

            SetPrivateFieldValue(manager, "playerCumBar", 99f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 17f);
            bool completionRaised = false;
            manager.OnPlayerCumReached += () => completionRaised = true;

            manager.OnRoleplayThrust();

            Assert.AreEqual(100f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(18f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.IsTrue(completionRaised);
            Assert.IsFalse(manager.IsRoleplayButtholePlaybackActive);
            Assert.AreEqual(0, manager.GetSessionData().roleplayButtholeOrgasmCount);

            manager.StartThrusting(Base.Character.Skills.SkillType.Hand, false);
            SetPrivateFieldValue(manager, "playerCumBar", 0f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 99f);
            manager.OnRoleplayThrust();

            Assert.AreEqual(1f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(0f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(1, manager.GetSessionData().roleplayButtholeOrgasmCount);
        }
        finally
        {
            instanceField?.SetValue(null, previousInstance);
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void EggLoopUsesTheSameSkillBasedAccountingAsOtherRoleplayLoops()
    {
        var gameObject = new GameObject("Roleplay Butthole Egg bar test");
        FieldInfo instanceField = typeof(SexSimulationManager).GetField(
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousInstance = instanceField?.GetValue(null);

        try
        {
            instanceField?.SetValue(null, null);
            var manager = gameObject.AddComponent<SexSimulationManager>();
            manager.SetCurrentPosition(SexPositionConfigType.RoleplayButthole);
            manager.StartThrusting(Base.Character.Skills.SkillType.Hand, false);

            var player = new Base.Character.Player("Roleplay Butthole Egg test player");
            object cumSkill = player.GetSkill(Base.Character.Skills.SkillType.Cum);
            object handSkill = player.GetSkill(Base.Character.Skills.SkillType.Hand);
            SetPrivateFieldValue(cumSkill, "_currentValue", 30);
            SetPrivateFieldValue(handSkill, "_currentValue", 40);
            SetPrivateFieldValue(manager, "_cachedPlayer", player);

            SetPrivateFieldValue(manager, "playerCumBar", 10f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 20f);
            manager.OnRoleplayButtholeEggLoopComplete();

            Assert.AreEqual(13f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(24f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(1, manager.GetSessionData().buttholeOperationCount);

            bool completionRaised = false;
            manager.OnPlayerCumReached += () => completionRaised = true;
            SetPrivateFieldValue(manager, "playerCumBar", 98f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 99f);
            manager.OnRoleplayButtholeEggLoopComplete();

            Assert.AreEqual(100f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(0f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.IsTrue(completionRaised);
            Assert.IsFalse(manager.IsRoleplayButtholePlaybackActive);
            Assert.AreEqual(1, manager.GetSessionData().roleplayButtholeOrgasmCount);
            Assert.AreEqual(2, manager.GetSessionData().buttholeOperationCount);
        }
        finally
        {
            instanceField?.SetValue(null, previousInstance);
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void AnimatorControllerMatchesRoleplayButtholeContract()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(controller);
        Assert.AreEqual(1, controller.layers.Length);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Assert.AreEqual(10, stateMachine.states.Length);
        Assert.AreEqual("Roleplay Butthole Start", stateMachine.defaultState.name);

        AssertParameter(controller, "SlowLoop", AnimatorControllerParameterType.Bool);
        AssertParameter(controller, "FastLoop", AnimatorControllerParameterType.Bool);
        AssertParameter(controller, "SkipCumming2", AnimatorControllerParameterType.Bool);
        AssertParameter(controller, "FingerMassage", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "HandGrabBoth", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "Egg", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "EggOut", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "Cum", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "Stop", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "SlowValue", AnimatorControllerParameterType.Float);
        AssertParameter(controller, "FastValue", AnimatorControllerParameterType.Float);
        Assert.AreEqual(11, controller.parameters.Length);

        string[] expectedBlendOrder =
        {
            "Roleplay Butthole Hand - Finger 1",
            "Roleplay Butthole Hand - Finger 2",
            "Roleplay Butthole Tongue - Lick",
            "Roleplay Butthole Tongue - Insert",
            "Roleplay Butthole Toy - Pen",
            "Roleplay Butthole Toy - Cucumber"
        };
        AssertBlendTree(
            GetState(stateMachine, "Roleplay Butthole Slow"),
            "SlowValue",
            " (Slow)",
            expectedBlendOrder);
        AssertBlendTree(
            GetState(stateMachine, "Roleplay Butthole Fast"),
            "FastValue",
            " (Fast)",
            expectedBlendOrder);

        var expectedDestinations = new Dictionary<string, string[]>
        {
            ["Roleplay Butthole Start"] = new[]
            {
                "Roleplay Butthole Slow", "Roleplay Butthole Fast",
                "Roleplay Butthole Hand - Finger Massage",
                "Roleplay Butthole Hand - Hand Grab Both",
                "Roleplay Butthole Toy - Egg (Insert)"
            },
            ["Roleplay Butthole Slow"] = new[]
            {
                "Roleplay Butthole Fast", "Roleplay Butthole Hand - Finger Massage",
                "Roleplay Butthole Hand - Hand Grab Both",
                "Roleplay Butthole Toy - Egg (Insert)",
                "Roleplay Butthole Cumming 1", "Roleplay Butthole Start"
            },
            ["Roleplay Butthole Fast"] = new[]
            {
                "Roleplay Butthole Slow", "Roleplay Butthole Hand - Finger Massage",
                "Roleplay Butthole Hand - Hand Grab Both",
                "Roleplay Butthole Toy - Egg (Insert)",
                "Roleplay Butthole Cumming 1", "Roleplay Butthole Start"
            },
            ["Roleplay Butthole Hand - Finger Massage"] = new[]
            {
                "Roleplay Butthole Slow", "Roleplay Butthole Fast",
                "Roleplay Butthole Hand - Hand Grab Both",
                "Roleplay Butthole Toy - Egg (Insert)",
                "Roleplay Butthole Cumming 1", "Roleplay Butthole Start"
            },
            ["Roleplay Butthole Hand - Hand Grab Both"] = new[]
            {
                "Roleplay Butthole Slow", "Roleplay Butthole Fast",
                "Roleplay Butthole Hand - Finger Massage",
                "Roleplay Butthole Toy - Egg (Insert)",
                "Roleplay Butthole Cumming 1", "Roleplay Butthole Start"
            },
            ["Roleplay Butthole Toy - Egg (Insert)"] = new[]
            {
                "Roleplay Butthole Toy - Egg (Insert Loop)",
                "Roleplay Butthole Toy - Egg (Out 1)"
            },
            ["Roleplay Butthole Toy - Egg (Insert Loop)"] =
                new[] { "Roleplay Butthole Toy - Egg (Out 1)" },
            ["Roleplay Butthole Toy - Egg (Out 1)"] = new[]
            {
                "Roleplay Butthole Start", "Roleplay Butthole Cumming 2"
            },
            ["Roleplay Butthole Cumming 1"] =
                new[] { "Roleplay Butthole Cumming 2" },
            ["Roleplay Butthole Cumming 2"] =
                new[] { "Roleplay Butthole Start" }
        };

        foreach (KeyValuePair<string, string[]> expected in expectedDestinations)
        {
            AnimatorState state = GetState(stateMachine, expected.Key);
            string[] actual = state.transitions
                .Select(transition => transition.destinationState?.name)
                .Where(name => name != null)
                .ToArray();
            CollectionAssert.AreEquivalent(expected.Value, actual, expected.Key);
        }

        Assert.AreEqual(
            36,
            stateMachine.states.Sum(child => child.state.transitions.Length));

        AssertImmediateTransition(stateMachine, "Roleplay Butthole Start", "Roleplay Butthole Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Butthole Start", "Roleplay Butthole Fast",
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Butthole Start", "Roleplay Butthole Hand - Finger Massage",
            ("FingerMassage", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Butthole Start", "Roleplay Butthole Hand - Hand Grab Both",
            ("HandGrabBoth", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Butthole Start", "Roleplay Butthole Toy - Egg (Insert)",
            ("Egg", AnimatorConditionMode.If));

        AssertBlendStateTransitions(stateMachine, "Roleplay Butthole Slow", "SlowLoop", "Roleplay Butthole Fast", "FastLoop");
        AssertBlendStateTransitions(stateMachine, "Roleplay Butthole Fast", "FastLoop", "Roleplay Butthole Slow", "SlowLoop");

        AssertStandaloneTransitions(
            stateMachine,
            "Roleplay Butthole Hand - Finger Massage",
            "Roleplay Butthole Hand - Hand Grab Both",
            "HandGrabBoth");
        AssertStandaloneTransitions(
            stateMachine,
            "Roleplay Butthole Hand - Hand Grab Both",
            "Roleplay Butthole Hand - Finger Massage",
            "FingerMassage");

        AssertImmediateTransition(
            stateMachine,
            "Roleplay Butthole Toy - Egg (Insert)",
            "Roleplay Butthole Toy - Egg (Out 1)",
            ("EggOut", AnimatorConditionMode.If));
        AssertAutomaticTransition(
            stateMachine,
            "Roleplay Butthole Toy - Egg (Insert)",
            "Roleplay Butthole Toy - Egg (Insert Loop)");
        AssertImmediateTransition(
            stateMachine,
            "Roleplay Butthole Toy - Egg (Insert Loop)",
            "Roleplay Butthole Toy - Egg (Out 1)",
            ("EggOut", AnimatorConditionMode.If));
        AssertAutomaticTransition(
            stateMachine,
            "Roleplay Butthole Toy - Egg (Out 1)",
            "Roleplay Butthole Start",
            ("SkipCumming2", AnimatorConditionMode.If));
        AssertAutomaticTransition(
            stateMachine,
            "Roleplay Butthole Toy - Egg (Out 1)",
            "Roleplay Butthole Cumming 2",
            0f,
            0f,
            ("SkipCumming2", AnimatorConditionMode.IfNot));
        AssertAutomaticTransition(
            stateMachine,
            "Roleplay Butthole Cumming 1",
            "Roleplay Butthole Cumming 2");
        AssertImmediateTransition(
            stateMachine,
            "Roleplay Butthole Cumming 2",
            "Roleplay Butthole Start",
            ("Stop", AnimatorConditionMode.If));

        AssertTransitionOrderPrefix(
            stateMachine,
            "Roleplay Butthole Toy - Egg (Insert)",
            "Roleplay Butthole Toy - Egg (Out 1)",
            "Roleplay Butthole Toy - Egg (Insert Loop)");

        Assert.AreEqual(
            1,
            GetState(stateMachine, "Roleplay Butthole Start")
                .behaviours.OfType<RoleplayButtholeStartStateBehaviour>().Count());
        Assert.AreEqual(
            1,
            GetState(stateMachine, "Roleplay Butthole Toy - Egg (Insert Loop)")
                .behaviours.OfType<RoleplayButtholeEggLoopStateBehaviour>().Count());
        Assert.AreEqual(
            1,
            GetState(stateMachine, "Roleplay Butthole Cumming 2")
                .behaviours.OfType<RoleplayButtholeCumming2StateBehaviour>().Count());

        Dictionary<string, AnimationClip> clips = GetControllerClips(stateMachine);
        Assert.AreEqual(20, clips.Count);
        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            bool shouldLoop = !NonLoopingClips.Contains(pair.Key);
            bool loopTime = AnimationUtility.GetAnimationClipSettings(pair.Value).loopTime;
            Assert.AreEqual(shouldLoop, loopTime, $"Unexpected loop setting for {pair.Key}");

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(pair.Value);
            Assert.AreEqual(1, events.Count(evt => evt.functionName == "InstanceId"), pair.Key);
        }

        foreach (AnimationClip clip in GetBlendClips(stateMachine))
        {
            AssertSingleEvent(clip, "OnThrust");
        }

        AssertSingleEvent(clips["Roleplay Butthole Hand - Finger Massage"], "OnThrust");
        AssertSingleEvent(clips["Roleplay Butthole Hand - Hand Grab Both"], "OnThrust");
        AssertSingleEvent(clips["Roleplay Butthole Toy - Egg (Insert Loop)"], "OnEggLoopComplete");

        Assert.IsFalse(clips.ContainsKey("Roleplay Butthole Hand - Hand Grab Left"));
        Assert.IsFalse(clips.ContainsKey("Roleplay Butthole Hand - Hand Grab Right"));
        Assert.IsFalse(clips.ContainsKey("Roleplay Butthole Toy - Lotion (Insert 1)"));
        Assert.IsFalse(clips.ContainsKey("Roleplay Butthole Toy - Lotion (Insert 2)"));

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);
        Animator animator = prefab.GetComponent<Animator>();
        Assert.IsNotNull(animator);
        Assert.AreSame(controller, animator.runtimeAnimatorController);
        Assert.IsNotNull(prefab.GetComponent<Live2DMotionController>());
        Assert.IsNotNull(prefab.GetComponent<RoleplayButtholeAnimationEventReceiver>());
    }

    [Test]
    public void GameSceneHasCompleteRoleplayButtholeBindings()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            GameObject menu = scene.GetRootGameObjects().Single(root => root.name == "Menu");
            Transform navigationTransform = menu.transform.Find("Simulation/Navigation");
            Assert.IsNotNull(navigationTransform);

            var navigation = navigationTransform.GetComponent<SimulationNavigationPanel>();
            Assert.IsNotNull(navigation);
            var navigationSo = new SerializedObject(navigation);

            string[] requiredButtonFields =
            {
                "roleplaySlowButton", "roleplayFastButton", "roleplayStopButton",
                "roleplayHandButton", "roleplayTongueButton", "roleplaySexToyButton",
                "roleplayAnimation1Button", "roleplayAnimation2Button",
                "roleplayAnimation3Button", "roleplayAnimation4Button"
            };
            foreach (string fieldName in requiredButtonFields)
            {
                SerializedProperty property = navigationSo.FindProperty(fieldName);
                Assert.IsNotNull(property, fieldName);
                Assert.IsNotNull(property.objectReferenceValue, fieldName);
            }

            Transform positionsRoot = menu.transform.Find("Simulation/Positions/Positions");
            Assert.IsNotNull(positionsRoot);
            Transform buttholeButtonTransform = positionsRoot.Find("Foreplay Butthole");
            Assert.IsNotNull(buttholeButtonTransform);
            Button buttholeButton = buttholeButtonTransform.GetComponent<Button>();
            Assert.IsNotNull(buttholeButton);

            SexSceneUnlockPanelController unlockController =
                menu.GetComponentInChildren<SexSceneUnlockPanelController>(true);
            Assert.IsNotNull(unlockController);
            InvokePrivateMethod(unlockController, "BindSimulationPositionButtons");
            var simulationButtons =
                (IDictionary)GetPrivateFieldValue(unlockController, "simulationButtons");
            Assert.IsTrue(simulationButtons.Contains(SexSceneType.RoleplayButthole));
            Assert.AreSame(
                buttholeButton,
                simulationButtons[SexSceneType.RoleplayButthole]);

            Transform live2DTransform = menu.transform.Find("Live2D");
            Assert.IsNotNull(live2DTransform);
            var live2DPanel = live2DTransform.GetComponent<Live2DPanel>();
            Assert.IsNotNull(live2DPanel);
            var live2DSo = new SerializedObject(live2DPanel);
            SerializedProperty controllerProperty =
                live2DSo.FindProperty("roleplayButtholeMotionController");
            Assert.IsNotNull(controllerProperty);
            Assert.IsNotNull(controllerProperty.objectReferenceValue);

            Transform roleplayModel = live2DTransform.Find("Roleplay Butthole");
            Assert.IsNotNull(roleplayModel);
            Assert.IsNotNull(roleplayModel.GetComponent<Animator>());
            Live2DMotionController sceneMotionController =
                roleplayModel.GetComponent<Live2DMotionController>();
            Assert.IsNotNull(sceneMotionController);
            Assert.AreSame(sceneMotionController, controllerProperty.objectReferenceValue);
            Assert.AreSame(
                sceneMotionController,
                live2DPanel.GetSimulationMotionController(
                    SexPositionConfigType.RoleplayButthole));
            Assert.IsNotNull(roleplayModel.GetComponent<RoleplayButtholeAnimationEventReceiver>());
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void AssertBlendStateTransitions(
        AnimatorStateMachine stateMachine,
        string sourceState,
        string sourceLoop,
        string otherBlendState,
        string otherLoop)
    {
        AssertImmediateTransition(stateMachine, sourceState, otherBlendState,
            (sourceLoop, AnimatorConditionMode.IfNot),
            (otherLoop, AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Hand - Finger Massage",
            (sourceLoop, AnimatorConditionMode.IfNot),
            ("FingerMassage", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Hand - Hand Grab Both",
            (sourceLoop, AnimatorConditionMode.IfNot),
            ("HandGrabBoth", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Toy - Egg (Insert)",
            (sourceLoop, AnimatorConditionMode.IfNot),
            ("Egg", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Cumming 1",
            (sourceLoop, AnimatorConditionMode.IfNot),
            ("Cum", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Start",
            ("Stop", AnimatorConditionMode.If));
    }

    private static void AssertStandaloneTransitions(
        AnimatorStateMachine stateMachine,
        string sourceState,
        string otherStandaloneState,
        string otherStandaloneTrigger)
    {
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Fast",
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, otherStandaloneState,
            (otherStandaloneTrigger, AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Toy - Egg (Insert)",
            ("Egg", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Cumming 1",
            ("Cum", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, sourceState, "Roleplay Butthole Start",
            ("Stop", AnimatorConditionMode.If));
    }

    private static Button CreateButton(GameObject parent, string name)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Button));
        buttonObject.transform.SetParent(parent.transform, false);
        return buttonObject.GetComponent<Button>();
    }

    private static AnimatorStateTransition GetTransition(
        AnimatorStateMachine stateMachine,
        string sourceName,
        string destinationName)
    {
        AnimatorStateTransition[] matches = GetState(stateMachine, sourceName).transitions
            .Where(transition => transition.destinationState?.name == destinationName)
            .ToArray();
        Assert.AreEqual(
            1,
            matches.Length,
            $"Expected exactly one transition from {sourceName} to {destinationName}");
        return matches[0];
    }

    private static void AssertImmediateTransition(
        AnimatorStateMachine stateMachine,
        string sourceName,
        string destinationName,
        params (string parameter, AnimatorConditionMode mode)[] expectedConditions)
    {
        AnimatorStateTransition transition =
            GetTransition(stateMachine, sourceName, destinationName);
        AssertConditions(transition, expectedConditions);
        Assert.IsFalse(transition.hasExitTime, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(0f, transition.exitTime, 0.0001f, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(0f, transition.duration, 0.0001f, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(0f, transition.offset, 0.0001f, $"{sourceName} -> {destinationName}");
        Assert.IsTrue(transition.hasFixedDuration, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(
            TransitionInterruptionSource.None,
            transition.interruptionSource,
            $"{sourceName} -> {destinationName}");
    }

    private static void AssertAutomaticTransition(
        AnimatorStateMachine stateMachine,
        string sourceName,
        string destinationName,
        params (string parameter, AnimatorConditionMode mode)[] expectedConditions)
    {
        AssertAutomaticTransition(
            stateMachine,
            sourceName,
            destinationName,
            1f,
            0.25f,
            expectedConditions);
    }

    private static void AssertAutomaticTransition(
        AnimatorStateMachine stateMachine,
        string sourceName,
        string destinationName,
        float expectedExitTime,
        float expectedDuration,
        params (string parameter, AnimatorConditionMode mode)[] expectedConditions)
    {
        AnimatorStateTransition transition =
            GetTransition(stateMachine, sourceName, destinationName);
        AssertConditions(transition, expectedConditions);
        Assert.IsTrue(transition.hasExitTime, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(expectedExitTime, transition.exitTime, 0.0001f, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(expectedDuration, transition.duration, 0.0001f, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(0f, transition.offset, 0.0001f, $"{sourceName} -> {destinationName}");
        Assert.IsTrue(transition.hasFixedDuration, $"{sourceName} -> {destinationName}");
        Assert.AreEqual(
            TransitionInterruptionSource.None,
            transition.interruptionSource,
            $"{sourceName} -> {destinationName}");
    }

    private static void AssertTransitionOrderPrefix(
        AnimatorStateMachine stateMachine,
        string sourceName,
        params string[] destinationNames)
    {
        AnimatorStateTransition[] transitions = GetState(stateMachine, sourceName).transitions;
        Assert.GreaterOrEqual(transitions.Length, destinationNames.Length, sourceName);
        for (int index = 0; index < destinationNames.Length; index++)
        {
            Assert.AreEqual(
                destinationNames[index],
                transitions[index].destinationState?.name,
                $"{sourceName} transition priority {index}");
        }
    }

    private static void AssertConditions(
        AnimatorStateTransition transition,
        params (string parameter, AnimatorConditionMode mode)[] expectedConditions)
    {
        Assert.AreEqual(expectedConditions.Length, transition.conditions.Length);
        foreach ((string parameter, AnimatorConditionMode mode) in expectedConditions)
        {
            AnimatorCondition[] matches = transition.conditions
                .Where(condition => condition.parameter == parameter && condition.mode == mode)
                .ToArray();
            Assert.AreEqual(1, matches.Length, $"Missing {mode} condition for {parameter}");
            Assert.AreEqual(0f, matches[0].threshold, 0.0001f, parameter);
        }
    }

    private static void AssertPrivateFieldString(
        object target,
        string fieldName,
        string expected)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        Assert.AreEqual(expected, field.GetValue(target)?.ToString(), fieldName);
    }

    private static void AssertPrivateFieldValue(
        object target,
        string fieldName,
        object expected)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        Assert.AreEqual(expected, field.GetValue(target), fieldName);
    }

    private static object GetPrivateFieldValue(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        return field.GetValue(target);
    }

    private static void SetPrivateFieldValue(
        object target,
        string fieldName,
        object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        field.SetValue(target, value);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, methodName);
        method.Invoke(target, null);
    }

    private static void AssertParameter(
        AnimatorController controller,
        string name,
        AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter parameter =
            controller.parameters.SingleOrDefault(candidate => candidate.name == name);
        Assert.IsNotNull(parameter, name);
        Assert.AreEqual(type, parameter.type, name);
    }

    private static AnimatorState GetState(AnimatorStateMachine stateMachine, string name)
    {
        AnimatorState state = stateMachine.states
            .Select(child => child.state)
            .SingleOrDefault(candidate => candidate.name == name);
        Assert.IsNotNull(state, name);
        return state;
    }

    private static void AssertBlendTree(
        AnimatorState state,
        string expectedParameter,
        string suffix,
        IReadOnlyList<string> expectedBaseNames)
    {
        Assert.IsInstanceOf<BlendTree>(state.motion);
        var tree = (BlendTree)state.motion;
        Assert.AreEqual(BlendTreeType.Simple1D, tree.blendType);
        Assert.AreEqual(expectedParameter, tree.blendParameter);
        Assert.AreEqual(expectedBaseNames.Count, tree.children.Length);

        for (int index = 0; index < expectedBaseNames.Count; index++)
        {
            ChildMotion child = tree.children[index];
            Assert.AreEqual(index, child.threshold, 0.001f, $"{state.name} threshold {index}");
            Assert.AreEqual(expectedBaseNames[index] + suffix, child.motion.name);
        }
    }

    private static Dictionary<string, AnimationClip> GetControllerClips(
        AnimatorStateMachine stateMachine)
    {
        var clips = new Dictionary<string, AnimationClip>();
        foreach (AnimatorState state in stateMachine.states.Select(child => child.state))
        {
            AddMotionClips(state.motion, clips);
        }
        return clips;
    }

    private static IEnumerable<AnimationClip> GetBlendClips(
        AnimatorStateMachine stateMachine)
    {
        return new[] { "Roleplay Butthole Slow", "Roleplay Butthole Fast" }
            .Select(name => (BlendTree)GetState(stateMachine, name).motion)
            .SelectMany(tree => tree.children)
            .Select(child => (AnimationClip)child.motion);
    }

    private static void AddMotionClips(
        Motion motion,
        IDictionary<string, AnimationClip> clips)
    {
        if (motion is AnimationClip clip)
        {
            clips[clip.name] = clip;
            return;
        }

        if (motion is BlendTree tree)
        {
            foreach (ChildMotion child in tree.children)
            {
                AddMotionClips(child.motion, clips);
            }
        }
    }

    private static void AssertSingleEvent(AnimationClip clip, string functionName)
    {
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        Assert.AreEqual(1, events.Count(evt => evt.functionName == functionName), clip.name);
    }
}
