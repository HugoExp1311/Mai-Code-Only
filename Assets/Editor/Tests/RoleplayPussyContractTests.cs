using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventBus;
using Live2D;
using NUnit.Framework;
using TMPro;
using UI.SexPosition;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoleplayPussyContractTests
{
    private const string ControllerPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Pussy/Roleplay Pussy.controller";

    private const string PrefabPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Pussy/Roleplay Pussy.prefab";

    private const string ScenePath = "Assets/Scenes/Game.unity";

    private static readonly HashSet<string> NonLoopingClips = new()
    {
        "Roleplay Pussy Toy Egg Vib - Insert Start",
        "Roleplay Pussy Toy Egg Vib - Cumming",
        "Roleplay Pussy Cumming",
        "Roleplay Pussy Cumming - Up"
    };

    [Test]
    public void FactoryCreatesRoleplayConfigWithoutChangingLegacyEnumValues()
    {
        Assert.AreEqual(0, (int)SexPositionConfigType.Missionary);
        Assert.AreEqual(1, (int)SexPositionConfigType.Doggy);
        Assert.AreEqual(2, (int)SexPositionConfigType.Cowgirl);
        Assert.AreEqual(3, (int)SexPositionConfigType.Standing);
        Assert.AreEqual(4, (int)SexPositionConfigType.RoleplayPussy);

        SexPositionConfig config =
            SexPositionConfigFactory.Create(SexPositionConfigType.RoleplayPussy);
        config.Initialize();

        Assert.IsInstanceOf<RoleplayPussyPositionConfig>(config);
        Assert.AreEqual("Roleplay Pussy", config.GetPositionName());
        Assert.AreEqual(SimulationState.Active, config.GetInitialState());
        Assert.AreEqual(3, config.GetRoleplayAnimationButtonCount());

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
    public void RoleplaySelectionMapMatchesNumberedButtonContract()
    {
        MethodInfo getBlendIndex = typeof(RoleplayPussyPositionConfig).GetMethod(
            "GetBlendIndex",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(getBlendIndex);

        AssertBlendIndex(getBlendIndex, RoleplayCategory.Hand, 1, 0);
        AssertBlendIndex(getBlendIndex, RoleplayCategory.Hand, 2, 1);
        AssertBlendIndex(getBlendIndex, RoleplayCategory.Hand, 3, 2);
        AssertBlendIndex(getBlendIndex, RoleplayCategory.Tongue, 1, 3);
        AssertBlendIndex(getBlendIndex, RoleplayCategory.Tongue, 2, 4);
        AssertBlendIndex(getBlendIndex, RoleplayCategory.Tongue, 3, 5);

        AssertPrivateConstant("CucumberBlendIndex", 6);
        AssertPrivateConstant("DildoBlendIndex", 7);

        var config = new RoleplayPussyPositionConfig();
        FieldInfo categoryField = typeof(RoleplayPussyPositionConfig).GetField(
            "currentCategory",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(categoryField);

        categoryField.SetValue(config, RoleplayCategory.Hand);
        Assert.AreEqual(3, config.GetRoleplayAnimationButtonCount());
        categoryField.SetValue(config, RoleplayCategory.Tongue);
        Assert.AreEqual(3, config.GetRoleplayAnimationButtonCount());
        categoryField.SetValue(config, RoleplayCategory.SexToy);
        Assert.AreEqual(4, config.GetRoleplayAnimationButtonCount());
    }

    [Test]
    public void AnimationEventGateRejectsInactiveAndTransitioningPlayback()
    {
        MethodInfo isPlaybackEventAllowed = typeof(RoleplayPussyAnimationEventReceiver).GetMethod(
            "IsPlaybackEventAllowed",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(isPlaybackEventAllowed);

        Assert.IsFalse((bool)isPlaybackEventAllowed.Invoke(null, new object[] { false, false }));
        Assert.IsFalse((bool)isPlaybackEventAllowed.Invoke(null, new object[] { false, true }));
        Assert.IsFalse((bool)isPlaybackEventAllowed.Invoke(null, new object[] { true, true }));
        Assert.IsTrue((bool)isPlaybackEventAllowed.Invoke(null, new object[] { true, false }));
    }

    [Test]
    public void RuntimeSelectionStartsSlowByDefaultAndKeepsInnerActionsInterchangeable()
    {
        var gameObject = new GameObject("Roleplay runtime switch test");
        try
        {
            var panel = gameObject.AddComponent<SimulationNavigationPanel>();
            Button slowButton = CreateButton(gameObject, "Roleplay Slow");
            Button fastButton = CreateButton(gameObject, "Roleplay Fast");
            SetPrivateFieldValue(panel, "roleplaySlowButton", slowButton);
            SetPrivateFieldValue(panel, "roleplayFastButton", fastButton);

            RoleplayPussyPositionConfig config = panel.ConfigureRoleplayTestBench(null);

            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);

            // Numbered actions and speeds do nothing until a category is locked.
            config.OnRoleplayAnimationSelected(panel, 1);
            config.OnThrustStarted(panel, false);
            AssertPrivateFieldString(config, "playbackMode", "Start");
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.SexToy);
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);

            // Categories remain locked until Stop.
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            AssertPrivateFieldString(config, "currentCategory", "SexToy");

            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Hitachi");
            AssertPrivateFieldValue(config, "currentAnimationNumber", 1);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", true);
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);

            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "EggInserted");
            Assert.IsTrue(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Hitachi");

            // Cucumber and Dildo start Slow immediately and may be switched freely.
            config.OnRoleplayAnimationSelected(panel, 3);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 6);
            Assert.IsTrue(slowButton.interactable);
            Assert.IsTrue(fastButton.interactable);

            config.OnRoleplayAnimationSelected(panel, 4);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 7);

            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "EggInserted");
            config.OnThrustStarted(panel, false);
            AssertPrivateFieldString(config, "playbackMode", "EggWorkPending");
            config.OnRoleplayEggWorkEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "EggWork");
            Assert.IsTrue(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);

            config.OnRoleplayAnimationSelected(panel, 3);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            config.OnThrustStarted(panel, true);
            AssertPrivateFieldString(config, "playbackMode", "Fast");
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "Hitachi");

            config.OnThrustStopped(panel);
            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);
            AssertPrivateFieldString(config, "playbackMode", "ReturningToStart");
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);
            config.OnRoleplayStartEntered(panel);
            AssertPrivateFieldString(config, "playbackMode", "Start");

            // Hand starts Slow on action selection, and a new action returns Fast to Slow.
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldString(config, "currentCategory", "Hand");
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);
            config.OnThrustStarted(panel, true);
            AssertPrivateFieldString(config, "playbackMode", "Start");
            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 1);
            AssertPrivateFieldValue(config, "currentAnimationNumber", 2);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", true);
            config.OnThrustStarted(panel, true);
            AssertPrivateFieldString(config, "playbackMode", "Fast");
            config.OnRoleplayAnimationSelected(panel, 3);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 2);

            config.OnThrustStopped(panel);
            config.OnRoleplayStartEntered(panel);

            // Tongue actions follow the same immediate-Slow rule.
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Tongue);
            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 4);
            Assert.IsTrue(slowButton.interactable);
            Assert.IsTrue(fastButton.interactable);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void RandomizedRoleplayOpeningCompletesIntoMissionaryOnlyAfterStop()
    {
        const string pussyKey = "SexSceneUnlocked_RoleplayPussy";
        const string buttholeKey = "SexSceneUnlocked_RoleplayButthole";
        const string blowjobKey = "SexSceneUnlocked_Blowjob";
        var previousUnlocks = new Dictionary<string, (bool exists, int value)>
        {
            [pussyKey] = (PlayerPrefs.HasKey(pussyKey), PlayerPrefs.GetInt(pussyKey)),
            [buttholeKey] = (PlayerPrefs.HasKey(buttholeKey), PlayerPrefs.GetInt(buttholeKey)),
            [blowjobKey] = (PlayerPrefs.HasKey(blowjobKey), PlayerPrefs.GetInt(blowjobKey))
        };
        var gameObject = new GameObject("Roleplay phase handoff test");
        FieldInfo transitionInstanceField = typeof(TransitionPanel).GetField(
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousTransitionInstance = transitionInstanceField?.GetValue(null);

        try
        {
            PlayerPrefs.SetInt(pussyKey, 1);
            PlayerPrefs.SetInt(buttholeKey, 0);
            PlayerPrefs.SetInt(blowjobKey, 0);
            transitionInstanceField?.SetValue(null, null);
            var panel = gameObject.AddComponent<SimulationNavigationPanel>();
            InvokePrivateMethod(panel, "BeginRoleplayOpening");

            AssertPrivateFieldString(panel, "positionType", "RoleplayPussy");
            AssertPrivateFieldValue(panel, "isRoleplayOpeningActive", true);

            var config = (RoleplayPussyPositionConfig)GetPrivateFieldValue(
                panel,
                "currentPositionConfig");
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);

            string category = GetPrivateFieldValue(config, "currentCategory").ToString();
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(
                config,
                "playbackMode",
                category == RoleplayCategory.SexToy.ToString() ? "Hitachi" : "Slow");
            string playbackBeforePlayerLimit =
                GetPrivateFieldValue(config, "playbackMode").ToString();

            InvokePrivateMethod(panel, "HandlePlayerCumReached");
            AssertPrivateFieldValue(panel, "isRoleplayOpeningComplete", true);
            Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
            AssertPrivateFieldString(config, "playbackMode", playbackBeforePlayerLimit);
            AssertPrivateFieldString(panel, "positionType", "RoleplayPussy");

            InvokePrivateMethod(panel, "HandleRoleplayStopClicked");
            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);
            AssertPrivateFieldString(config, "playbackMode", "ReturningToStart");
            AssertPrivateFieldString(panel, "positionType", "Missionary");
            Assert.AreEqual(SimulationState.Idle, panel.CurrentState);
            AssertPrivateFieldValue(panel, "isRoleplayOpeningActive", false);
        }
        finally
        {
            foreach (KeyValuePair<string, (bool exists, int value)> pair in previousUnlocks)
            {
                if (pair.Value.exists)
                {
                    PlayerPrefs.SetInt(pair.Key, pair.Value.value);
                }
                else
                {
                    PlayerPrefs.DeleteKey(pair.Key);
                }
            }
            transitionInstanceField?.SetValue(null, previousTransitionInstance);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void RoleplayToyLoopUsesSharedBarsAndCountsOnlyMaiOrgasm()
    {
        var gameObject = new GameObject("Roleplay player cum test");
        FieldInfo instanceField = typeof(SexSimulationManager).GetField(
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousInstance = instanceField?.GetValue(null);

        try
        {
            instanceField?.SetValue(null, null);
            var manager = gameObject.AddComponent<SexSimulationManager>();
            manager.SetCurrentPosition(SexPositionConfigType.RoleplayPussy);
            manager.StartThrusting(Base.Character.Skills.SkillType.Hand, false);

            var player = new Base.Character.Player("Roleplay Pussy accounting test player");
            object cumSkill = player.GetSkill(Base.Character.Skills.SkillType.Cum);
            object handSkill = player.GetSkill(Base.Character.Skills.SkillType.Hand);
            SetPrivateFieldValue(cumSkill, "_currentValue", 10);
            SetPrivateFieldValue(handSkill, "_currentValue", 10);
            SetPrivateFieldValue(manager, "_cachedPlayer", player);

            SetPrivateFieldValue(manager, "playerCumBar", 99f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 17f);
            bool completionRaised = false;
            manager.OnPlayerCumReached += () => completionRaised = true;

            manager.OnRoleplayToyLoopComplete();

            Assert.AreEqual(100f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(18f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.IsTrue(completionRaised);
            Assert.IsFalse(manager.IsRoleplayPlaybackActive);
            Assert.AreEqual(0, manager.GetSessionData().roleplayPussyOrgasmCount);

            manager.StartThrusting(Base.Character.Skills.SkillType.Hand, false);
            SetPrivateFieldValue(manager, "playerCumBar", 0f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 99f);
            manager.OnRoleplayToyLoopComplete();

            Assert.AreEqual(1f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(0f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(1, manager.GetSessionData().roleplayPussyOrgasmCount);

            manager.BeginSexPhase();
            Assert.AreEqual(0f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(0f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(1, manager.GetSessionData().roleplayPussyOrgasmCount);
        }
        finally
        {
            instanceField?.SetValue(null, previousInstance);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void RoleplayToMissionaryHandoffOccursAtFadeMidpoint()
    {
        var panelObject = new GameObject("Roleplay fade handoff panel");
        var transitionObject = new GameObject("Roleplay fade handoff transition");
        FieldInfo transitionInstanceField = typeof(TransitionPanel).GetField(
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousTransitionInstance = transitionInstanceField?.GetValue(null);

        try
        {
            var transitionPanel = transitionObject.AddComponent<TransitionPanel>();
            transitionInstanceField?.SetValue(null, transitionPanel);

            var panel = panelObject.AddComponent<SimulationNavigationPanel>();
            panel.ConfigureRoleplayTestBench(null);
            panel.SetState(SimulationState.AfterCumming);
            SetPrivateFieldValue(panel, "isRoleplayOpeningActive", true);
            SetPrivateFieldValue(panel, "isRoleplayOpeningComplete", true);

            InvokePrivateMethod(panel, "BeginRoleplayToSexHandoff");
            AssertPrivateFieldString(panel, "positionType", "RoleplayPussy");

            EventBus<TransitionMidPointEvent>.Raise(new TransitionMidPointEvent());
            AssertPrivateFieldString(panel, "positionType", "Missionary");
            Assert.AreEqual(SimulationState.Idle, panel.CurrentState);

            EventBus<TransitionCompleteEvent>.Raise(new TransitionCompleteEvent());
            AssertPrivateFieldValue(panel, "isRoleplayToSexTransitioning", false);
        }
        finally
        {
            transitionInstanceField?.SetValue(null, previousTransitionInstance);
            UnityEngine.Object.DestroyImmediate(panelObject);
            UnityEngine.Object.DestroyImmediate(transitionObject);
        }
    }

    [Test]
    public void AnimatorControllerMatchesRoleplayContract()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(controller);
        Assert.AreEqual(1, controller.layers.Length);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Assert.AreEqual("Roleplay Pussy Start", stateMachine.defaultState.name);

        AssertParameter(controller, "SlowLoop", AnimatorControllerParameterType.Bool);
        AssertParameter(controller, "FastLoop", AnimatorControllerParameterType.Bool);
        AssertParameter(controller, "EggVibWorkLoop", AnimatorControllerParameterType.Bool);
        AssertParameter(controller, "Hitachi", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "EggVib", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "Stop", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "Cum", AnimatorControllerParameterType.Trigger);
        AssertParameter(controller, "SlowValue", AnimatorControllerParameterType.Float);
        AssertParameter(controller, "FastValue", AnimatorControllerParameterType.Float);
        Assert.AreEqual(9, controller.parameters.Length);

        string[] expectedBlendOrder =
        {
            "Roleplay Pussy Hand Clit Finger",
            "Roleplay Pussy Hand 2 Finger",
            "Roleplay Pussy Hand Moc Cua",
            "Roleplay Pussy Tongue Clit",
            "Roleplay Pussy Tongue Lick",
            "Roleplay Pussy Tongue Insert",
            "Roleplay Pussy Toy Cucumber",
            "Roleplay Pussy Toy Didlo"
        };
        AssertBlendTree(GetState(stateMachine, "Roleplay Slow"), "SlowValue", " - Slow", expectedBlendOrder);
        AssertBlendTree(GetState(stateMachine, "Roleplay Fast"), "FastValue", " - Fast", expectedBlendOrder);

        var expectedDestinations = new Dictionary<string, string[]>
        {
            ["Roleplay Pussy Start"] = new[]
            {
                "Roleplay Slow", "Roleplay Fast", "Roleplay Pussy Toy Hitachi",
                "Roleplay Pussy Toy Egg Vib - Insert Start"
            },
            ["Roleplay Pussy Toy Hitachi"] = new[]
            {
                "Roleplay Pussy Toy Egg Vib - Insert Start",
                "Roleplay Slow", "Roleplay Fast",
                "Roleplay Pussy Start", "Roleplay Pussy Cumming"
            },
            ["Roleplay Pussy Toy Egg Vib - Insert Start"] = new[]
            {
                "Roleplay Pussy Toy Hitachi",
                "Roleplay Slow", "Roleplay Fast",
                "Roleplay Pussy Toy Egg Vib - Insert After", "Roleplay Pussy Start"
            },
            ["Roleplay Pussy Toy Egg Vib - Insert After"] = new[]
            {
                "Roleplay Pussy Toy Hitachi",
                "Roleplay Slow", "Roleplay Fast",
                "Roleplay Pussy Start", "Roleplay Pussy Toy Egg Vib - Work"
            },
            ["Roleplay Pussy Toy Egg Vib - Work"] = new[]
            {
                "Roleplay Pussy Toy Hitachi",
                "Roleplay Slow", "Roleplay Fast",
                "Roleplay Pussy Start", "Roleplay Pussy Toy Egg Vib - Cumming"
            },
            ["Roleplay Pussy Toy Egg Vib - Cumming"] =
                new[] { "Roleplay Pussy After Cumming - Up" },
            ["Roleplay Pussy Cumming"] =
                new[] { "Roleplay Pussy After Cumming" },
            ["Roleplay Pussy Cumming - Up"] =
                new[] { "Roleplay Pussy After Cumming - Up" },
            ["Roleplay Pussy After Cumming"] =
                new[] { "Roleplay Pussy Start" },
            ["Roleplay Pussy After Cumming - Up"] =
                new[] { "Roleplay Pussy Start" },
            ["Roleplay Slow"] = new[]
            {
                "Roleplay Pussy Toy Hitachi", "Roleplay Pussy Toy Egg Vib - Insert Start",
                "Roleplay Pussy Start", "Roleplay Fast", "Roleplay Pussy Cumming"
            },
            ["Roleplay Fast"] = new[]
            {
                "Roleplay Pussy Toy Hitachi", "Roleplay Pussy Toy Egg Vib - Insert Start",
                "Roleplay Pussy Start", "Roleplay Slow", "Roleplay Pussy Cumming - Up"
            }
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
            39,
            stateMachine.states.Sum(child => child.state.transitions.Length));

        AssertImmediateTransition(stateMachine, "Roleplay Pussy Start", "Roleplay Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Start", "Roleplay Fast",
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Start", "Roleplay Pussy Toy Hitachi",
            ("Hitachi", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Start", "Roleplay Pussy Toy Egg Vib - Insert Start",
            ("EggVib", AnimatorConditionMode.If));

        AssertImmediateTransition(stateMachine, "Roleplay Slow", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Slow", "Roleplay Fast",
            ("SlowLoop", AnimatorConditionMode.IfNot),
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Slow", "Roleplay Pussy Cumming",
            ("SlowLoop", AnimatorConditionMode.IfNot),
            ("Cum", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Slow", "Roleplay Pussy Toy Hitachi",
            ("SlowLoop", AnimatorConditionMode.IfNot),
            ("Hitachi", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Slow", "Roleplay Pussy Toy Egg Vib - Insert Start",
            ("SlowLoop", AnimatorConditionMode.IfNot),
            ("EggVib", AnimatorConditionMode.If));

        AssertImmediateTransition(stateMachine, "Roleplay Fast", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Fast", "Roleplay Slow",
            ("FastLoop", AnimatorConditionMode.IfNot),
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Fast", "Roleplay Pussy Cumming - Up",
            ("FastLoop", AnimatorConditionMode.IfNot),
            ("Cum", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Fast", "Roleplay Pussy Toy Hitachi",
            ("FastLoop", AnimatorConditionMode.IfNot),
            ("Hitachi", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Fast", "Roleplay Pussy Toy Egg Vib - Insert Start",
            ("FastLoop", AnimatorConditionMode.IfNot),
            ("EggVib", AnimatorConditionMode.If));

        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Hitachi", "Roleplay Pussy Toy Egg Vib - Insert Start",
            ("EggVib", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Hitachi", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Hitachi", "Roleplay Pussy Cumming",
            ("Cum", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Hitachi", "Roleplay Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Hitachi", "Roleplay Fast",
            ("FastLoop", AnimatorConditionMode.If));

        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert Start", "Roleplay Pussy Toy Hitachi",
            ("Hitachi", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert Start", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert Start", "Roleplay Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert Start", "Roleplay Fast",
            ("FastLoop", AnimatorConditionMode.If));
        AssertExitTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert Start", "Roleplay Pussy Toy Egg Vib - Insert After");

        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert After", "Roleplay Pussy Toy Hitachi",
            ("Hitachi", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert After", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert After", "Roleplay Pussy Toy Egg Vib - Work",
            ("EggVibWorkLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert After", "Roleplay Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert After", "Roleplay Fast",
            ("FastLoop", AnimatorConditionMode.If));

        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Work", "Roleplay Pussy Toy Hitachi",
            ("EggVibWorkLoop", AnimatorConditionMode.IfNot),
            ("Hitachi", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Work", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Work", "Roleplay Pussy Toy Egg Vib - Cumming",
            ("EggVibWorkLoop", AnimatorConditionMode.IfNot),
            ("Cum", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Work", "Roleplay Slow",
            ("EggVibWorkLoop", AnimatorConditionMode.IfNot),
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy Toy Egg Vib - Work", "Roleplay Fast",
            ("EggVibWorkLoop", AnimatorConditionMode.IfNot),
            ("FastLoop", AnimatorConditionMode.If));

        AssertExitTransition(
            stateMachine,
            "Roleplay Pussy Toy Egg Vib - Cumming",
            "Roleplay Pussy After Cumming - Up",
            0f,
            0f);
        AssertExitTransition(stateMachine, "Roleplay Pussy Cumming", "Roleplay Pussy After Cumming");
        AssertExitTransition(stateMachine, "Roleplay Pussy Cumming - Up", "Roleplay Pussy After Cumming - Up");
        AssertImmediateTransition(stateMachine, "Roleplay Pussy After Cumming", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));
        AssertImmediateTransition(stateMachine, "Roleplay Pussy After Cumming - Up", "Roleplay Pussy Start",
            ("Stop", AnimatorConditionMode.If));

        AssertTransitionOrderPrefix(stateMachine, "Roleplay Slow",
            "Roleplay Pussy Toy Hitachi", "Roleplay Pussy Toy Egg Vib - Insert Start");
        AssertTransitionOrderPrefix(stateMachine, "Roleplay Fast",
            "Roleplay Pussy Toy Hitachi", "Roleplay Pussy Toy Egg Vib - Insert Start");
        AssertTransitionOrderPrefix(stateMachine, "Roleplay Pussy Toy Hitachi",
            "Roleplay Pussy Toy Egg Vib - Insert Start", "Roleplay Slow", "Roleplay Fast");
        AssertTransitionOrderPrefix(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert Start",
            "Roleplay Pussy Toy Hitachi", "Roleplay Slow", "Roleplay Fast");
        AssertTransitionOrderPrefix(stateMachine, "Roleplay Pussy Toy Egg Vib - Insert After",
            "Roleplay Pussy Toy Hitachi", "Roleplay Slow", "Roleplay Fast");
        AssertTransitionOrderPrefix(stateMachine, "Roleplay Pussy Toy Egg Vib - Work",
            "Roleplay Pussy Toy Hitachi", "Roleplay Slow", "Roleplay Fast");

        Assert.AreEqual(
            1,
            GetState(stateMachine, "Roleplay Pussy Start")
                .behaviours.OfType<RoleplayPussyStartStateBehaviour>().Count());
        Assert.AreEqual(
            1,
            GetState(stateMachine, "Roleplay Pussy Toy Egg Vib - Work")
                .behaviours.OfType<RoleplayPussyEggWorkStateBehaviour>().Count());
        Assert.AreEqual(
            0,
            GetState(stateMachine, "Roleplay Pussy Toy Egg Vib - Cumming")
                .behaviours.OfType<RoleplayPussyAfterCummingStateBehaviour>().Count());

        Assert.AreEqual(
            1,
            GetState(stateMachine, "Roleplay Pussy After Cumming")
                .behaviours.OfType<RoleplayPussyAfterCummingStateBehaviour>().Count());
        Assert.AreEqual(
            1,
            GetState(stateMachine, "Roleplay Pussy After Cumming - Up")
                .behaviours.OfType<RoleplayPussyAfterCummingStateBehaviour>().Count());

        Dictionary<string, AnimationClip> clips = GetControllerClips(stateMachine);
        Assert.AreEqual(26, clips.Count);
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
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            Assert.AreEqual(1, events.Count(evt => evt.functionName == "OnThrust"), clip.name);
        }

        AssertSingleEvent(clips["Roleplay Pussy Toy Hitachi"], "OnToyLoopComplete");
        AssertSingleEvent(clips["Roleplay Pussy Toy Egg Vib - Work"], "OnToyLoopComplete");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);
        Assert.IsNotNull(prefab.GetComponent<Animator>());
        Assert.IsNotNull(prefab.GetComponent<RoleplayPussyAnimationEventReceiver>());
    }

    [Test]
    public void GameSceneHasCompleteRoleplayBindingsAndFourButtonLayout()
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

            Transform button3 = navigationTransform.Find("Roleplay - Animation 3");
            Transform button4 = navigationTransform.Find("Roleplay - Animation 4");
            Assert.IsNotNull(button3);
            Assert.IsNotNull(button4);
            Assert.That(button3.localPosition.x, Is.EqualTo(756.5f).Within(0.01f));
            Assert.That(button3.localPosition.y, Is.EqualTo(-150f).Within(0.01f));
            Assert.That(button4.localPosition.x, Is.EqualTo(858.5f).Within(0.01f));
            Assert.That(button4.localPosition.y, Is.EqualTo(-150f).Within(0.01f));
            Assert.AreEqual("4", button4.GetComponentInChildren<TMP_Text>(true).text);
            Assert.IsNotNull(button4.GetComponent<Button>());

            Transform live2DTransform = menu.transform.Find("Live2D");
            var live2DPanel = live2DTransform.GetComponent<Live2DPanel>();
            var live2DSo = new SerializedObject(live2DPanel);
            Assert.IsNotNull(
                live2DSo.FindProperty("roleplayPussyMotionController").objectReferenceValue);

            Transform roleplayModel = live2DTransform.Find("Roleplay Pussy");
            Assert.IsNotNull(roleplayModel.GetComponent<RoleplayPussyAnimationEventReceiver>());
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void AssertBlendIndex(
        MethodInfo method,
        RoleplayCategory category,
        int animationNumber,
        int expected)
    {
        object result = method.Invoke(null, new object[] { category, animationNumber });
        Assert.AreEqual(expected, (int)result);
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

    private static void AssertExitTransition(
        AnimatorStateMachine stateMachine,
        string sourceName,
        string destinationName,
        float expectedExitTime = 1f,
        float expectedDuration = 0.25f)
    {
        AnimatorStateTransition transition =
            GetTransition(stateMachine, sourceName, destinationName);
        AssertConditions(transition);
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

    private static void AssertPrivateConstant(string fieldName, int expected)
    {
        FieldInfo field = typeof(RoleplayPussyPositionConfig).GetField(
            fieldName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        Assert.AreEqual(expected, (int)field.GetRawConstantValue());
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

    private static IEnumerable<AnimationClip> GetBlendClips(AnimatorStateMachine stateMachine)
    {
        return new[] { "Roleplay Slow", "Roleplay Fast" }
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
