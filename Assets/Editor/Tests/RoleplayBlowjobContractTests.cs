using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Base;
using Base.Character;
using Base.Character.Action;
using Base.Character.Skills;
using Base.Character.Stats;
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

public class RoleplayBlowjobContractTests
{
    private const string ControllerPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Blowjob/Roleplay Blowjob.controller";

    private const string PrefabPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Blowjob/Roleplay Blowjob.prefab";

    private const string MotionFolder =
        "Assets/Image/Bed Sex Scene/Roleplay - Blowjob/Motions/";

    private const string ScenePath = "Assets/Scenes/Game.unity";

    private static readonly string[] SlowClips =
    {
        "Blowjob Roleplay - Handjob 1 Slow",
        "Blowjob Roleplay - Handjob 2 Slow",
        "Blowjob Roleplay - Blowjob 1 Slow",
        "Blowjob Roleplay - Blowjob 2 Slow",
        "Blowjob Roleplay - Blowjob 3 Slow"
    };

    private static readonly string[] FastClips =
    {
        "Blowjob Roleplay - Handjob 1 Fast",
        "Blowjob Roleplay - Handjob 2 Fast",
        "Blowjob Roleplay - Blowjob 1 Fast",
        "Blowjob Roleplay - Blowjob 2 Fast",
        "Blowjob Roleplay - Blowjob 3 Fast"
    };

    private static readonly string[] TongueClips =
    {
        "Blowjob Roleplay - Kiss Dick",
        "Blowjob Roleplay - Tongue Move 1",
        "Blowjob Roleplay - Tongue Move 2"
    };

    private static readonly HashSet<string> NonLoopingClips = new()
    {
        "Blowjob Roleplay - Cum Handjob",
        "Blowjob Roleplay - Cum Blowjob 1",
        "Blowjob Roleplay - Cum Blowjob 2",
        "Blowjob Roleplay - Cum Blowjob 3",
        "Blowjob Roleplay - Cum Face"
    };

    [Test]
    public void FactoryEnumAndSemanticCategoriesMatchTheBlowjobContract()
    {
        Assert.AreEqual(0, (int)SexPositionConfigType.Missionary);
        Assert.AreEqual(1, (int)SexPositionConfigType.Doggy);
        Assert.AreEqual(2, (int)SexPositionConfigType.Cowgirl);
        Assert.AreEqual(3, (int)SexPositionConfigType.Standing);
        Assert.AreEqual(4, (int)SexPositionConfigType.RoleplayPussy);
        Assert.AreEqual(5, (int)SexPositionConfigType.RoleplayButthole);
        Assert.AreEqual(6, (int)SexPositionConfigType.RoleplayBlowjob);

        SexPositionConfig config =
            SexPositionConfigFactory.Create(SexPositionConfigType.RoleplayBlowjob);
        config.Initialize();

        Assert.IsInstanceOf<RoleplayBlowjobPositionConfig>(config);
        Assert.AreEqual("Roleplay Blowjob", config.GetPositionName());
        Assert.AreEqual(SimulationState.Active, config.GetInitialState());
        Assert.AreEqual(2, config.GetRoleplayAnimationButtonCount());
        Assert.AreEqual(RoleplayCategory.Hand, config.GetRoleplayCategoryForButton(1));
        Assert.AreEqual(RoleplayCategory.Blowjob, config.GetRoleplayCategoryForButton(2));
        Assert.AreEqual(RoleplayCategory.Tongue, config.GetRoleplayCategoryForButton(3));
        Assert.AreEqual("Handjob", config.GetRoleplayCategoryButtonLabel(1));
        Assert.AreEqual("Blowjob", config.GetRoleplayCategoryButtonLabel(2));
        Assert.AreEqual("Tongue", config.GetRoleplayCategoryButtonLabel(3));

        ButtonVisibilitySet active = config.GetButtonVisibility(SimulationState.Active);
        Assert.IsTrue(active.slowButton);
        Assert.IsTrue(active.fastButton);
        Assert.IsTrue(active.stopButton);
        Assert.IsTrue(active.roleplaySelectorButtons);
        Assert.IsTrue(active.finishButton);

        ButtonVisibilitySet after = config.GetButtonVisibility(SimulationState.AfterCumming);
        Assert.IsFalse(after.slowButton);
        Assert.IsFalse(after.fastButton);
        Assert.IsTrue(after.stopButton);
        Assert.IsFalse(after.roleplaySelectorButtons);
        Assert.IsTrue(after.finishButton);
    }

    [Test]
    public void RuntimeStartsBlendActionsSlowAndKeepsOnlyInnerActionsInterchangeable()
    {
        var root = new GameObject("Roleplay Blowjob runtime contract");
        try
        {
            var panel = root.AddComponent<SimulationNavigationPanel>();
            Button slowButton = CreateButton(root, "Slow");
            Button fastButton = CreateButton(root, "Fast");
            Button animation1 = CreateButton(root, "Animation 1");
            Button animation2 = CreateButton(root, "Animation 2");
            Button animation3 = CreateButton(root, "Animation 3");
            Button animation4 = CreateButton(root, "Animation 4");
            SetPrivateFieldValue(panel, "roleplaySlowButton", slowButton);
            SetPrivateFieldValue(panel, "roleplayFastButton", fastButton);
            SetPrivateFieldValue(panel, "roleplayAnimation1Button", animation1);
            SetPrivateFieldValue(panel, "roleplayAnimation2Button", animation2);
            SetPrivateFieldValue(panel, "roleplayAnimation3Button", animation3);
            SetPrivateFieldValue(panel, "roleplayAnimation4Button", animation4);

            var config = (RoleplayBlowjobPositionConfig)panel.ConfigureRoleplayTestBench(
                SexPositionConfigType.RoleplayBlowjob,
                null);

            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldString(config, "playbackMode", "Start");

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            Assert.AreEqual(2, config.GetRoleplayAnimationButtonCount());
            Assert.IsTrue(animation1.gameObject.activeSelf);
            Assert.IsTrue(animation2.gameObject.activeSelf);
            Assert.IsFalse(animation3.gameObject.activeSelf);
            Assert.IsFalse(animation4.gameObject.activeSelf);

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

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Tongue);
            AssertPrivateFieldString(config, "currentCategory", "Hand");

            ReturnToStart(config, panel);
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Blowjob);
            Assert.AreEqual(3, config.GetRoleplayAnimationButtonCount());
            Assert.IsTrue(animation1.gameObject.activeSelf);
            Assert.IsTrue(animation2.gameObject.activeSelf);
            Assert.IsTrue(animation3.gameObject.activeSelf);
            Assert.IsFalse(animation4.gameObject.activeSelf);
            for (int animationNumber = 1; animationNumber <= 3; animationNumber++)
            {
                config.OnRoleplayAnimationSelected(panel, animationNumber);
                AssertPrivateFieldString(config, "playbackMode", "Slow");
                AssertPrivateFieldValue(
                    config,
                    "currentBlendIndex",
                    animationNumber + 1);
            }

            ReturnToStart(config, panel);
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Tongue);
            Assert.AreEqual(3, config.GetRoleplayAnimationButtonCount());
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldString(config, "playbackMode", "KissDick");
            config.OnRoleplayAnimationSelected(panel, 2);
            AssertPrivateFieldString(config, "playbackMode", "TongueMove1");
            config.OnRoleplayAnimationSelected(panel, 3);
            AssertPrivateFieldString(config, "playbackMode", "TongueMove2");
            Assert.IsFalse(slowButton.interactable);
            Assert.IsFalse(fastButton.interactable);
            Assert.IsFalse(animation4.gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PlayerCumSelectsFiveVisibleRoutesWhileMaiCumDoesNotInterruptPlayback()
    {
        var routes = new[]
        {
            (RoleplayCategory.Hand, 1, "CumHandjob"),
            (RoleplayCategory.Blowjob, 1, "CumBlowjob1"),
            (RoleplayCategory.Blowjob, 2, "CumBlowjob2"),
            (RoleplayCategory.Blowjob, 3, "CumBlowjob3"),
            (RoleplayCategory.Tongue, 1, "CumFace")
        };

        foreach ((RoleplayCategory category, int animation, string trigger) in routes)
        {
            var root = new GameObject("Roleplay Blowjob route " + trigger);
            try
            {
                var panel = root.AddComponent<SimulationNavigationPanel>();
                var config = (RoleplayBlowjobPositionConfig)panel.ConfigureRoleplayTestBench(
                    SexPositionConfigType.RoleplayBlowjob,
                    null);
                config.OnRoleplayCategorySelected(panel, category);
                config.OnRoleplayAnimationSelected(panel, animation);

                string playbackBeforeMaiCum =
                    GetPrivateFieldValue(config, "playbackMode").ToString();
                config.OnMaiOrgasm(panel);
                AssertPrivateFieldString(config, "playbackMode", playbackBeforeMaiCum);
                Assert.AreEqual(SimulationState.Thrusting, panel.CurrentState);
                Assert.AreEqual(trigger, InvokePrivateMethod(config, "GetCumTrigger"));

                config.OnPlayerCumReached(panel);
                AssertPrivateFieldString(config, "playbackMode", "Cumming");
                Assert.AreEqual(SimulationState.Transitioning, panel.CurrentState);

                config.OnAfterCummingEntered(panel);
                AssertPrivateFieldString(config, "playbackMode", "AfterCumming");
                Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);

                config.OnThrustStopped(panel);
                AssertPrivateFieldString(config, "playbackMode", "ReturningToStart");
                config.OnRoleplayStartEntered(panel);
                AssertPrivateFieldString(config, "playbackMode", "Start");
                Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    [Test]
    public void SharedAccountingUsesStaminaCumMaiAndMouthOperationContracts()
    {
        var managerObject = new GameObject("Roleplay Blowjob accounting manager");
        FieldInfo instanceField = GetField(
            typeof(SexSimulationManager),
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousInstance = instanceField.GetValue(null);

        try
        {
            instanceField.SetValue(null, null);
            var manager = managerObject.AddComponent<SexSimulationManager>();
            var player = new Player("Roleplay Blowjob accounting player");
            SetSkillValue(player, SkillType.Size, 1000);
            SetSkillValue(player, SkillType.Cum, 30);
            SetSkillValue(player, SkillType.Hand, 20);
            SetSkillValue(player, SkillType.Tongue, 40);
            SetPrivateFieldValue(manager, "_cachedPlayer", player);
            manager.SetCurrentPosition(SexPositionConfigType.RoleplayBlowjob);

            int initialStamina = player.GetStamina();
            manager.StartThrusting(SkillType.Hand, false);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(3f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(2f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(initialStamina - 1, player.GetStamina());
            Assert.AreEqual(1, manager.GetSessionData().mouthOperationCount);

            manager.StartThrusting(SkillType.Tongue, true);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(9f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(6f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(initialStamina - 3, player.GetStamina());
            Assert.AreEqual(2, manager.GetSessionData().mouthOperationCount);

            int maiCumEvents = 0;
            int playerCumEvents = 0;
            manager.OnMaiOrgasmReached += () => maiCumEvents++;
            manager.OnPlayerCumReached += () => playerCumEvents++;
            SetSkillValue(player, SkillType.Tongue, 10);
            SetPrivateFieldValue(manager, "playerCumBar", 0f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 99f);
            manager.StartThrusting(SkillType.Tongue, false);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(1, maiCumEvents);
            Assert.AreEqual(0, playerCumEvents);
            Assert.AreEqual(1, manager.GetSessionData().roleplayBlowjobOrgasmCount);
            Assert.AreEqual(1, manager.GetSessionData().GetTotalRoleplayOrgasms());
            Assert.AreEqual(1, manager.GetSessionData().GetTotalOrgasms());
            Assert.AreEqual(25, manager.GetSessionData().CalculateRoleplaySexPoints());
            Assert.AreEqual(25, DefaultSettings.RoleplayBlowjobSexPoints);

            SetPrivateFieldValue(manager, "playerCumBar", 99f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 0f);
            manager.StartThrusting(SkillType.Hand, false);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(1, playerCumEvents);
            Assert.AreEqual(1, manager.GetSessionData().roleplayBlowjobOrgasmCount);
        }
        finally
        {
            instanceField.SetValue(null, previousInstance);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void RoleplayStaminaDepletionHandsOffToMissionaryAndRestoresStamina()
    {
        var managerObject = new GameObject("Roleplay Blowjob stamina manager");
        var panelObject = new GameObject("Roleplay Blowjob stamina panel");
        FieldInfo managerInstanceField = GetField(
            typeof(SexSimulationManager),
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo transitionInstanceField = GetField(
            typeof(TransitionPanel),
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousManager = managerInstanceField.GetValue(null);
        object previousTransition = transitionInstanceField.GetValue(null);

        try
        {
            managerInstanceField.SetValue(null, null);
            transitionInstanceField.SetValue(null, null);
            var manager = managerObject.AddComponent<SexSimulationManager>();
            managerInstanceField.SetValue(null, manager);
            var player = new Player("Roleplay Blowjob stamina player");
            SetSkillValue(player, SkillType.Size, 1000);
            SetSkillValue(player, SkillType.Cum, 10);
            SetSkillValue(player, SkillType.Hand, 10);
            player.DoAction(new CharacterActions.ChangeStat(
                RewardTarget.Player,
                BasicStats.Stamina,
                1 - player.GetStamina()));
            Assert.AreEqual(1, player.GetStamina());
            SetPrivateFieldValue(manager, "_cachedPlayer", player);

            var panel = panelObject.AddComponent<SimulationNavigationPanel>();
            var config = (RoleplayBlowjobPositionConfig)panel.ConfigureRoleplayTestBench(
                SexPositionConfigType.RoleplayBlowjob,
                null);
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            config.OnRoleplayAnimationSelected(panel, 1);
            manager.OnStaminaDepleted += () =>
                InvokePrivateMethod(panel, "HandleStaminaDepleted");

            manager.OnRoleplayLoopComplete();

            Assert.AreEqual(player.GetMaxStamina(), player.GetStamina());
            AssertPrivateFieldString(panel, "positionType", "Missionary");
            Assert.AreEqual(SimulationState.Idle, panel.CurrentState);
            Assert.IsFalse(manager.IsRoleplayPlaybackActive);
        }
        finally
        {
            managerInstanceField.SetValue(null, previousManager);
            transitionInstanceField.SetValue(null, previousTransition);
            UnityEngine.Object.DestroyImmediate(panelObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void OnlyUnlockedBlowjobIsChosenAndOnlyItsCategoryIsRandomized()
    {
        const string pussyKey = "SexSceneUnlocked_RoleplayPussy";
        const string buttholeKey = "SexSceneUnlocked_RoleplayButthole";
        const string blowjobKey = "SexSceneUnlocked_Blowjob";
        var previous = new Dictionary<string, (bool exists, int value)>
        {
            [pussyKey] = (PlayerPrefs.HasKey(pussyKey), PlayerPrefs.GetInt(pussyKey)),
            [buttholeKey] = (PlayerPrefs.HasKey(buttholeKey), PlayerPrefs.GetInt(buttholeKey)),
            [blowjobKey] = (PlayerPrefs.HasKey(blowjobKey), PlayerPrefs.GetInt(blowjobKey))
        };
        var panelObject = new GameObject("Roleplay Blowjob randomized opener");

        try
        {
            SexSceneUnlockService.SetUnlocked(SexSceneType.RoleplayPussy, false);
            SexSceneUnlockService.SetUnlocked(SexSceneType.RoleplayButthole, false);
            SexSceneUnlockService.SetUnlocked(SexSceneType.Blowjob, true);

            SexPositionConfigType selected = (SexPositionConfigType)InvokePrivateMethod(
                typeof(SimulationNavigationPanel),
                "GetRandomUnlockedRoleplayPosition");
            Assert.AreEqual(SexPositionConfigType.RoleplayBlowjob, selected);

            var panel = panelObject.AddComponent<SimulationNavigationPanel>();
            InvokePrivateMethod(panel, "BeginRoleplayOpening");
            AssertPrivateFieldString(panel, "positionType", "RoleplayBlowjob");
            var config = (RoleplayBlowjobPositionConfig)GetPrivateFieldValue(
                panel,
                "currentPositionConfig");
            Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldValue(config, "currentAnimationNumber", 0);
            AssertPrivateFieldValue(config, "hasSelectedAnimation", false);
            AssertPrivateFieldString(config, "playbackMode", "Start");
        }
        finally
        {
            foreach (KeyValuePair<string, (bool exists, int value)> pair in previous)
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
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void AnimationEventGateRejectsInactiveAndTransitioningPlayback()
    {
        MethodInfo method = typeof(RoleplayBlowjobAnimationEventReceiver).GetMethod(
            "IsPlaybackEventAllowed",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        Assert.IsFalse((bool)method.Invoke(null, new object[] { false, false }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { true, true }));
        Assert.IsTrue((bool)method.Invoke(null, new object[] { true, false }));
    }

    [Test]
    public void AnimatorControllerMatchesTheCompleteBlowjobContract()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(controller);
        Assert.AreEqual(1, controller.layers.Length);
        Assert.AreEqual(13, controller.parameters.Length);

        var expectedParameters = new Dictionary<string, AnimatorControllerParameterType>
        {
            ["SlowLoop"] = AnimatorControllerParameterType.Bool,
            ["FastLoop"] = AnimatorControllerParameterType.Bool,
            ["SlowValue"] = AnimatorControllerParameterType.Float,
            ["FastValue"] = AnimatorControllerParameterType.Float,
            ["KissDick"] = AnimatorControllerParameterType.Trigger,
            ["TongueMove1"] = AnimatorControllerParameterType.Trigger,
            ["TongueMove2"] = AnimatorControllerParameterType.Trigger,
            ["CumHandjob"] = AnimatorControllerParameterType.Trigger,
            ["CumBlowjob1"] = AnimatorControllerParameterType.Trigger,
            ["CumBlowjob2"] = AnimatorControllerParameterType.Trigger,
            ["CumBlowjob3"] = AnimatorControllerParameterType.Trigger,
            ["CumFace"] = AnimatorControllerParameterType.Trigger,
            ["Stop"] = AnimatorControllerParameterType.Trigger
        };
        CollectionAssert.AreEquivalent(
            expectedParameters.Keys,
            controller.parameters.Select(parameter => parameter.name));
        foreach (KeyValuePair<string, AnimatorControllerParameterType> expected in expectedParameters)
        {
            Assert.AreEqual(
                expected.Value,
                controller.parameters.Single(parameter => parameter.name == expected.Key).type,
                expected.Key);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Assert.AreEqual(14, stateMachine.states.Length);
        Assert.AreEqual(0, stateMachine.anyStateTransitions.Length);
        Assert.AreEqual("Blowjob Roleplay - Start", stateMachine.defaultState.name);
        AssertBlendTree(
            GetState(stateMachine, "Blowjob Roleplay - Slow"),
            "SlowValue",
            SlowClips);
        AssertBlendTree(
            GetState(stateMachine, "Blowjob Roleplay - Fast"),
            "FastValue",
            FastClips);

        var expectedDestinations = new Dictionary<string, string[]>
        {
            ["Blowjob Roleplay - Start"] = new[]
            {
                "Blowjob Roleplay - Slow",
                "Blowjob Roleplay - Fast",
                "Blowjob Roleplay - Kiss Dick",
                "Blowjob Roleplay - Tongue Move 1",
                "Blowjob Roleplay - Tongue Move 2"
            },
            ["Blowjob Roleplay - Slow"] = new[]
            {
                "Blowjob Roleplay - Fast",
                "Blowjob Roleplay - Cum Handjob",
                "Blowjob Roleplay - Cum Blowjob 1",
                "Blowjob Roleplay - Cum Blowjob 2",
                "Blowjob Roleplay - Cum Blowjob 3",
                "Blowjob Roleplay - Start"
            },
            ["Blowjob Roleplay - Fast"] = new[]
            {
                "Blowjob Roleplay - Slow",
                "Blowjob Roleplay - Cum Handjob",
                "Blowjob Roleplay - Cum Blowjob 1",
                "Blowjob Roleplay - Cum Blowjob 2",
                "Blowjob Roleplay - Cum Blowjob 3",
                "Blowjob Roleplay - Start"
            },
            ["Blowjob Roleplay - Kiss Dick"] = new[]
            {
                "Blowjob Roleplay - Tongue Move 1",
                "Blowjob Roleplay - Tongue Move 2",
                "Blowjob Roleplay - Cum Face",
                "Blowjob Roleplay - Start"
            },
            ["Blowjob Roleplay - Tongue Move 1"] = new[]
            {
                "Blowjob Roleplay - Kiss Dick",
                "Blowjob Roleplay - Tongue Move 2",
                "Blowjob Roleplay - Cum Face",
                "Blowjob Roleplay - Start"
            },
            ["Blowjob Roleplay - Tongue Move 2"] = new[]
            {
                "Blowjob Roleplay - Kiss Dick",
                "Blowjob Roleplay - Tongue Move 1",
                "Blowjob Roleplay - Cum Face",
                "Blowjob Roleplay - Start"
            },
            ["Blowjob Roleplay - Cum Handjob"] =
                new[] { "Blowjob Roleplay - Cum Handjob Loop" },
            ["Blowjob Roleplay - Cum Blowjob 1"] =
                new[] { "Blowjob Roleplay - Cum Blowjob Loop" },
            ["Blowjob Roleplay - Cum Blowjob 2"] =
                new[] { "Blowjob Roleplay - Cum Blowjob Loop" },
            ["Blowjob Roleplay - Cum Blowjob 3"] =
                new[] { "Blowjob Roleplay - Cum Blowjob Loop" },
            ["Blowjob Roleplay - Cum Face"] =
                new[] { "Blowjob Roleplay - Cum Face Loop" },
            ["Blowjob Roleplay - Cum Handjob Loop"] =
                new[] { "Blowjob Roleplay - Start" },
            ["Blowjob Roleplay - Cum Blowjob Loop"] =
                new[] { "Blowjob Roleplay - Start" },
            ["Blowjob Roleplay - Cum Face Loop"] =
                new[] { "Blowjob Roleplay - Start" }
        };
        foreach (KeyValuePair<string, string[]> expected in expectedDestinations)
        {
            CollectionAssert.AreEquivalent(
                expected.Value,
                GetState(stateMachine, expected.Key).transitions
                    .Select(transition => transition.destinationState?.name)
                    .Where(name => name != null),
                expected.Key);
        }
        Assert.AreEqual(
            37,
            stateMachine.states.Sum(child => child.state.transitions.Length));

        AssertImmediate(stateMachine, "Blowjob Roleplay - Start", "Blowjob Roleplay - Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Start", "Blowjob Roleplay - Fast",
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Start", "Blowjob Roleplay - Kiss Dick",
            ("KissDick", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Start", "Blowjob Roleplay - Tongue Move 1",
            ("TongueMove1", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Start", "Blowjob Roleplay - Tongue Move 2",
            ("TongueMove2", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Slow", "Blowjob Roleplay - Fast",
            ("SlowLoop", AnimatorConditionMode.IfNot),
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Fast", "Blowjob Roleplay - Slow",
            ("FastLoop", AnimatorConditionMode.IfNot),
            ("SlowLoop", AnimatorConditionMode.If));

        AssertImmediate(stateMachine, "Blowjob Roleplay - Kiss Dick", "Blowjob Roleplay - Tongue Move 1",
            ("TongueMove1", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Kiss Dick", "Blowjob Roleplay - Tongue Move 2",
            ("TongueMove2", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Tongue Move 1", "Blowjob Roleplay - Kiss Dick",
            ("KissDick", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Tongue Move 1", "Blowjob Roleplay - Tongue Move 2",
            ("TongueMove2", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Tongue Move 2", "Blowjob Roleplay - Kiss Dick",
            ("KissDick", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Blowjob Roleplay - Tongue Move 2", "Blowjob Roleplay - Tongue Move 1",
            ("TongueMove1", AnimatorConditionMode.If));

        foreach (string source in new[]
                 {
                     "Blowjob Roleplay - Slow",
                     "Blowjob Roleplay - Fast"
                 })
        {
            AssertImmediate(stateMachine, source, "Blowjob Roleplay - Cum Handjob",
                ("CumHandjob", AnimatorConditionMode.If));
            AssertImmediate(stateMachine, source, "Blowjob Roleplay - Cum Blowjob 1",
                ("CumBlowjob1", AnimatorConditionMode.If));
            AssertImmediate(stateMachine, source, "Blowjob Roleplay - Cum Blowjob 2",
                ("CumBlowjob2", AnimatorConditionMode.If));
            AssertImmediate(stateMachine, source, "Blowjob Roleplay - Cum Blowjob 3",
                ("CumBlowjob3", AnimatorConditionMode.If));
        }
        foreach (string source in new[]
                 {
                     "Blowjob Roleplay - Kiss Dick",
                     "Blowjob Roleplay - Tongue Move 1",
                     "Blowjob Roleplay - Tongue Move 2"
                 })
        {
            AssertImmediate(stateMachine, source, "Blowjob Roleplay - Cum Face",
                ("CumFace", AnimatorConditionMode.If));
        }

        AssertAutomatic(stateMachine, "Blowjob Roleplay - Cum Handjob", "Blowjob Roleplay - Cum Handjob Loop");
        AssertAutomatic(stateMachine, "Blowjob Roleplay - Cum Blowjob 1", "Blowjob Roleplay - Cum Blowjob Loop");
        AssertAutomatic(stateMachine, "Blowjob Roleplay - Cum Blowjob 2", "Blowjob Roleplay - Cum Blowjob Loop");
        AssertAutomatic(stateMachine, "Blowjob Roleplay - Cum Blowjob 3", "Blowjob Roleplay - Cum Blowjob Loop");
        AssertAutomatic(stateMachine, "Blowjob Roleplay - Cum Face", "Blowjob Roleplay - Cum Face Loop");

        foreach (string source in new[]
                 {
                     "Blowjob Roleplay - Slow",
                     "Blowjob Roleplay - Fast",
                     "Blowjob Roleplay - Kiss Dick",
                     "Blowjob Roleplay - Tongue Move 1",
                     "Blowjob Roleplay - Tongue Move 2",
                     "Blowjob Roleplay - Cum Handjob Loop",
                     "Blowjob Roleplay - Cum Blowjob Loop",
                     "Blowjob Roleplay - Cum Face Loop"
                 })
        {
            AssertImmediate(stateMachine, source, "Blowjob Roleplay - Start",
                ("Stop", AnimatorConditionMode.If));
        }

        Assert.AreEqual(
            1,
            GetState(stateMachine, "Blowjob Roleplay - Start")
                .behaviours.OfType<RoleplayBlowjobStartStateBehaviour>().Count());
        foreach (string stateName in new[]
                 {
                     "Blowjob Roleplay - Cum Handjob Loop",
                     "Blowjob Roleplay - Cum Blowjob Loop",
                     "Blowjob Roleplay - Cum Face Loop"
                 })
        {
            Assert.AreEqual(
                1,
                GetState(stateMachine, stateName)
                    .behaviours.OfType<RoleplayBlowjobAfterCummingStateBehaviour>().Count(),
                stateName);
        }

        Dictionary<string, AnimationClip> clips = GetControllerClips(stateMachine);
        Assert.AreEqual(22, clips.Count);
        var activeClips = new HashSet<string>(
            SlowClips.Concat(FastClips).Concat(TongueClips));
        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            bool shouldLoop = !NonLoopingClips.Contains(pair.Key);
            Assert.AreEqual(
                shouldLoop,
                AnimationUtility.GetAnimationClipSettings(pair.Value).loopTime,
                pair.Key);
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(pair.Value);
            Assert.AreEqual(1, events.Count(evt => evt.functionName == "InstanceId"), pair.Key);
            AnimationEvent[] callbacks = events
                .Where(evt => evt.functionName == "OnBlowjobLoopComplete")
                .ToArray();
            Assert.AreEqual(activeClips.Contains(pair.Key) ? 1 : 0, callbacks.Length, pair.Key);
            if (callbacks.Length == 1)
            {
                Assert.AreEqual(pair.Value.length * 0.9f, callbacks[0].time, 0.001f, pair.Key);
            }

            string motionJson = File.ReadAllText(MotionFolder + pair.Key + ".motion3.json");
            StringAssert.Contains(
                shouldLoop ? "\"Loop\": true" : "\"Loop\": false",
                motionJson,
                pair.Key);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);
        Animator animator = prefab.GetComponent<Animator>();
        Assert.IsNotNull(animator);
        Assert.AreSame(controller, animator.runtimeAnimatorController);
        Assert.IsNotNull(prefab.GetComponent<Live2DMotionController>());
        Assert.IsNotNull(prefab.GetComponent<RoleplayBlowjobAnimationEventReceiver>());
    }

    [Test]
    public void GameSceneAndTestBenchExposeTheCompleteBlowjobRouting()
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
            Transform model = menu.transform.Find("Live2D/Roleplay Blowjob");
            Assert.IsNotNull(model);
            Assert.IsNotNull(model.GetComponent<Animator>());
            Live2DMotionController motionController =
                model.GetComponent<Live2DMotionController>();
            Assert.IsNotNull(motionController);
            Assert.IsNotNull(model.GetComponent<RoleplayBlowjobAnimationEventReceiver>());

            Live2DPanel live2DPanel =
                menu.transform.Find("Live2D").GetComponent<Live2DPanel>();
            Assert.IsNotNull(live2DPanel);
            var live2DPanelSo = new SerializedObject(live2DPanel);
            SerializedProperty binding =
                live2DPanelSo.FindProperty("roleplayBlowjobMotionController");
            Assert.IsNotNull(binding);
            Assert.AreSame(motionController, binding.objectReferenceValue);
            Assert.AreSame(
                motionController,
                live2DPanel.GetSimulationMotionController(
                    SexPositionConfigType.RoleplayBlowjob));

            Transform navigationTransform = menu.transform.Find("Simulation/Navigation");
            Assert.IsNotNull(navigationTransform);
            var navigation = navigationTransform.GetComponent<SimulationNavigationPanel>();
            Assert.IsNotNull(navigation);
            var navigationSo = new SerializedObject(navigation);
            foreach (string fieldName in new[]
                     {
                         "roleplaySlowButton",
                         "roleplayFastButton",
                         "roleplayStopButton",
                         "roleplayHandButton",
                         "roleplayTongueButton",
                         "roleplaySexToyButton",
                         "roleplayAnimation1Button",
                         "roleplayAnimation2Button",
                         "roleplayAnimation3Button",
                         "roleplayAnimation4Button"
                     })
            {
                SerializedProperty property = navigationSo.FindProperty(fieldName);
                Assert.IsNotNull(property, fieldName);
                Assert.IsNotNull(property.objectReferenceValue, fieldName);
            }

            Transform positionButton =
                menu.transform.Find("Simulation/Positions/Positions/Blowjob");
            Assert.IsNotNull(positionButton);
            Assert.IsNotNull(positionButton.GetComponent<Button>());
            Assert.IsNotNull(menu.GetComponentInChildren<SexSceneUnlockPanelController>(true));

            string testBenchSource = File.ReadAllText(
                "Assets/Scripts/Live2D/Editor/Live2DTestBenchWindow.cs");
            StringAssert.Contains("IsRoleplayBlowjobLike", testBenchSource);
            StringAssert.Contains("SexPositionConfigType.RoleplayBlowjob", testBenchSource);
            StringAssert.Contains("GetRoleplayCategoryButtonLabel", testBenchSource);
            StringAssert.Contains("GetRoleplayCategoryForButton", testBenchSource);
            StringAssert.Contains("\"Player Cum 100%\"", testBenchSource);
            StringAssert.Contains("OnPlayerCumReached(roleplayPanel)", testBenchSource);
            StringAssert.Contains("Blowjob Roleplay - Cum Handjob Loop", testBenchSource);
            StringAssert.Contains("Blowjob Roleplay - Cum Blowjob Loop", testBenchSource);
            StringAssert.Contains("Blowjob Roleplay - Cum Face Loop", testBenchSource);

            string unlockPanelSource = File.ReadAllText(
                "Assets/Scripts/UI/SexSceneUnlockPanelController.cs");
            StringAssert.Contains("HandleRoleplayBlowjobPositionClicked", unlockPanelSource);
            StringAssert.Contains(
                "SexPositionConfigType.RoleplayBlowjob",
                unlockPanelSource);
        }
        finally
        {
            if (openedForTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void ReturnToStart(
        RoleplayBlowjobPositionConfig config,
        SimulationNavigationPanel panel)
    {
        config.OnThrustStopped(panel);
        config.OnRoleplayStartEntered(panel);
        Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
        AssertPrivateFieldString(config, "playbackMode", "Start");
    }

    private static Button CreateButton(GameObject parent, string name)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Button));
        buttonObject.transform.SetParent(parent.transform, false);
        return buttonObject.GetComponent<Button>();
    }

    private static void SetSkillValue(Player player, SkillType skillType, int value)
    {
        object skill = player.GetSkill(skillType);
        Assert.IsNotNull(skill, skillType.ToString());
        SetPrivateFieldValue(skill, "_currentValue", value);
    }

    private static AnimatorState GetState(
        AnimatorStateMachine stateMachine,
        string stateName)
    {
        return stateMachine.states
            .Select(child => child.state)
            .Single(state => state.name == stateName);
    }

    private static AnimatorStateTransition GetTransition(
        AnimatorStateMachine stateMachine,
        string sourceState,
        string destinationState)
    {
        AnimatorStateTransition[] matches = GetState(stateMachine, sourceState)
            .transitions
            .Where(transition => transition.destinationState != null &&
                                 transition.destinationState.name == destinationState)
            .ToArray();
        Assert.AreEqual(
            1,
            matches.Length,
            $"Expected exactly one transition {sourceState} -> {destinationState}");
        return matches.Single();
    }

    private static void AssertImmediate(
        AnimatorStateMachine stateMachine,
        string sourceState,
        string destinationState,
        params (string parameter, AnimatorConditionMode mode)[] conditions)
    {
        AnimatorStateTransition transition =
            GetTransition(stateMachine, sourceState, destinationState);
        Assert.IsFalse(transition.hasExitTime);
        Assert.AreEqual(0f, transition.duration, 0.0001f);
        Assert.AreEqual(0f, transition.offset, 0.0001f);
        Assert.IsTrue(transition.hasFixedDuration);
        Assert.AreEqual(TransitionInterruptionSource.None, transition.interruptionSource);
        Assert.IsFalse(transition.orderedInterruption);
        AssertConditions(transition, conditions);
    }

    private static void AssertAutomatic(
        AnimatorStateMachine stateMachine,
        string sourceState,
        string destinationState)
    {
        AnimatorStateTransition transition =
            GetTransition(stateMachine, sourceState, destinationState);
        Assert.AreEqual(0, transition.conditions.Length);
        Assert.IsTrue(transition.hasExitTime);
        Assert.AreEqual(1f, transition.exitTime, 0.0001f);
        Assert.AreEqual(0f, transition.duration, 0.0001f);
        Assert.AreEqual(0f, transition.offset, 0.0001f);
        Assert.IsTrue(transition.hasFixedDuration);
        Assert.AreEqual(TransitionInterruptionSource.None, transition.interruptionSource);
        Assert.IsFalse(transition.orderedInterruption);
    }

    private static void AssertConditions(
        AnimatorStateTransition transition,
        params (string parameter, AnimatorConditionMode mode)[] expected)
    {
        Assert.AreEqual(expected.Length, transition.conditions.Length);
        foreach ((string parameter, AnimatorConditionMode mode) in expected)
        {
            AnimatorCondition condition = transition.conditions.Single(candidate =>
                candidate.parameter == parameter && candidate.mode == mode);
            Assert.AreEqual(0f, condition.threshold, 0.0001f, parameter);
        }
    }

    private static void AssertBlendTree(
        AnimatorState state,
        string parameter,
        IReadOnlyList<string> expectedClips)
    {
        Assert.IsInstanceOf<BlendTree>(state.motion);
        var blendTree = (BlendTree)state.motion;
        Assert.AreEqual(BlendTreeType.Simple1D, blendTree.blendType);
        Assert.AreEqual(parameter, blendTree.blendParameter);
        Assert.IsFalse(blendTree.useAutomaticThresholds);
        Assert.AreEqual(expectedClips.Count, blendTree.children.Length);
        for (int index = 0; index < expectedClips.Count; index++)
        {
            Assert.AreEqual(expectedClips[index], blendTree.children[index].motion.name);
            Assert.AreEqual(index, blendTree.children[index].threshold, 0.0001f);
        }
    }

    private static Dictionary<string, AnimationClip> GetControllerClips(
        AnimatorStateMachine stateMachine)
    {
        var clips = new Dictionary<string, AnimationClip>();
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            AddMotionClips(child.state.motion, clips);
        }
        return clips;
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

        if (motion is BlendTree blendTree)
        {
            foreach (ChildMotion child in blendTree.children)
            {
                AddMotionClips(child.motion, clips);
            }
        }
    }

    private static void AssertPrivateFieldString(
        object target,
        string fieldName,
        string expected)
    {
        Assert.AreEqual(expected, GetPrivateFieldValue(target, fieldName)?.ToString());
    }

    private static void AssertPrivateFieldValue(
        object target,
        string fieldName,
        object expected)
    {
        Assert.AreEqual(expected, GetPrivateFieldValue(target, fieldName));
    }

    private static object GetPrivateFieldValue(object target, string fieldName)
    {
        FieldInfo field = GetField(
            target.GetType(),
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field.GetValue(target);
    }

    private static void SetPrivateFieldValue(
        object target,
        string fieldName,
        object value)
    {
        FieldInfo field = GetField(
            target.GetType(),
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    private static FieldInfo GetField(
        Type type,
        string fieldName,
        BindingFlags flags)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(fieldName, flags);
            if (field != null)
            {
                return field;
            }
        }

        Assert.Fail($"Field {fieldName} was not found on {type.FullName}.");
        return null;
    }

    private static object InvokePrivateMethod(
        object target,
        string methodName,
        params object[] arguments)
    {
        Type type = target as Type ?? target.GetType();
        BindingFlags flags = BindingFlags.NonPublic |
                             (target is Type ? BindingFlags.Static : BindingFlags.Instance);
        MethodInfo method = type.GetMethod(methodName, flags);
        Assert.IsNotNull(method, methodName);
        return method.Invoke(target is Type ? null : target, arguments);
    }
}
