using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Base;
using Base.Character;
using Base.Character.Skills;
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

public class RoleplayPaizuriContractTests
{
    private const string ControllerPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Paizuri/Paizuri.controller";

    private const string PrefabPath =
        "Assets/Image/Bed Sex Scene/Roleplay - Paizuri/Paizuri.prefab";

    private const string MotionFolder =
        "Assets/Image/Bed Sex Scene/Roleplay - Paizuri/Motions/";

    private const string ScenePath = "Assets/Scenes/Game.unity";

    private static readonly string[] BlendBaseNames =
    {
        "Paizuri Boob 1",
        "Paizuri Boob 2",
        "Paizuri Boob 3",
        "Paizuri Boob 4",
        "Paizuri Hand 1",
        "Paizuri Hand 2",
        "Paizuri Hand 3",
        "Paizuri Tongue 1",
        "Paizuri Tongue 2"
    };

    private static readonly string[] SlowClips =
        BlendBaseNames.Select(name => name + " - Slow").ToArray();

    private static readonly string[] FastClips =
        BlendBaseNames.Select(name => name + " - Fast").ToArray();

    private static readonly HashSet<string> NonLoopingClips = new()
    {
        "Paizuri Cumming Dick 1",
        "Paizuri Cumming Dick 2",
        "Paizuri Cumming Dick 3",
        "Paizuri Cumming Mai"
    };

    [Test]
    public void FactoryEnumAndSemanticCategoriesMatchThePaizuriContract()
    {
        Assert.AreEqual(0, (int)SexPositionConfigType.Missionary);
        Assert.AreEqual(1, (int)SexPositionConfigType.Doggy);
        Assert.AreEqual(2, (int)SexPositionConfigType.Cowgirl);
        Assert.AreEqual(3, (int)SexPositionConfigType.Standing);
        Assert.AreEqual(4, (int)SexPositionConfigType.RoleplayPussy);
        Assert.AreEqual(5, (int)SexPositionConfigType.RoleplayButthole);
        Assert.AreEqual(6, (int)SexPositionConfigType.RoleplayBlowjob);
        Assert.AreEqual(7, (int)SexPositionConfigType.RoleplayPaizuri);

        SexPositionConfig config =
            SexPositionConfigFactory.Create(SexPositionConfigType.RoleplayPaizuri);
        config.Initialize();

        Assert.IsInstanceOf<RoleplayPaizuriPositionConfig>(config);
        Assert.AreEqual("Roleplay Paizuri", config.GetPositionName());
        Assert.AreEqual(SimulationState.Active, config.GetInitialState());
        Assert.AreEqual(RoleplayCategory.Boob, config.GetRoleplayCategoryForButton(1));
        Assert.AreEqual(RoleplayCategory.Hand, config.GetRoleplayCategoryForButton(2));
        Assert.AreEqual(RoleplayCategory.Tongue, config.GetRoleplayCategoryForButton(3));
        Assert.AreEqual("Boob", config.GetRoleplayCategoryButtonLabel(1));
        Assert.AreEqual("Hand", config.GetRoleplayCategoryButtonLabel(2));
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
    public void RuntimeStartsEveryActionSlowAndLocksOnlyTheCategoryRow()
    {
        var root = new GameObject("Roleplay Paizuri runtime contract");
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

            var config = (RoleplayPaizuriPositionConfig)panel.ConfigureRoleplayTestBench(
                SexPositionConfigType.RoleplayPaizuri,
                null);

            Assert.IsTrue(config.AreRoleplayCategoriesAvailable());
            AssertPrivateFieldString(config, "playbackMode", "Start");

            AssertCategory(
                config,
                panel,
                RoleplayCategory.Boob,
                new[] { 0, 1, 2, 3 },
                animation1,
                animation2,
                animation3,
                animation4);
            config.OnThrustStarted(panel, true);
            AssertPrivateFieldString(config, "playbackMode", "Fast");
            config.OnRoleplayAnimationSelected(panel, 3);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(config, "currentBlendIndex", 2);
            Assert.IsTrue(slowButton.interactable);
            Assert.IsTrue(fastButton.interactable);
            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            AssertPrivateFieldString(config, "currentCategory", "Boob");

            ReturnToStart(config, panel);
            AssertCategory(
                config,
                panel,
                RoleplayCategory.Hand,
                new[] { 4, 5, 6 },
                animation1,
                animation2,
                animation3,
                animation4);

            ReturnToStart(config, panel);
            AssertCategory(
                config,
                panel,
                RoleplayCategory.Tongue,
                new[] { 7, 8 },
                animation1,
                animation2,
                animation3,
                animation4);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PlayerAndMaiCumChooseOnlyTheDocumentedRoutes()
    {
        var playerRoutes = new[]
        {
            (RoleplayCategory.Boob, 1, "CumDick1"),
            (RoleplayCategory.Boob, 2, "CumDick2"),
            (RoleplayCategory.Boob, 3, "CumDick1"),
            (RoleplayCategory.Boob, 4, "CumDick3")
        };

        foreach ((RoleplayCategory category, int animation, string trigger) in playerRoutes)
        {
            var root = new GameObject("Roleplay Paizuri route " + trigger);
            try
            {
                var panel = root.AddComponent<SimulationNavigationPanel>();
                var config = (RoleplayPaizuriPositionConfig)panel.ConfigureRoleplayTestBench(
                    SexPositionConfigType.RoleplayPaizuri,
                    null);
                config.OnRoleplayCategorySelected(panel, category);
                config.OnRoleplayAnimationSelected(panel, animation);
                Assert.AreEqual(trigger, InvokePrivateMethod(config, "GetPlayerCumTrigger"));

                config.OnMaiOrgasm(panel);
                AssertPrivateFieldString(config, "playbackMode", "Slow");
                Assert.AreEqual(SimulationState.Thrusting, panel.CurrentState);

                config.OnPlayerCumReached(panel);
                AssertPrivateFieldString(config, "playbackMode", "Cumming");
                Assert.AreEqual(SimulationState.Transitioning, panel.CurrentState);
                config.OnAfterCummingEntered(panel);
                AssertPrivateFieldString(config, "playbackMode", "AfterCumming");
                Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
                ReturnToStart(config, panel);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        foreach (RoleplayCategory category in new[]
                 {
                     RoleplayCategory.Hand,
                     RoleplayCategory.Tongue
                 })
        {
            var root = new GameObject("Roleplay Paizuri Mai route " + category);
            try
            {
                var panel = root.AddComponent<SimulationNavigationPanel>();
                var config = (RoleplayPaizuriPositionConfig)panel.ConfigureRoleplayTestBench(
                    SexPositionConfigType.RoleplayPaizuri,
                    null);
                config.OnRoleplayCategorySelected(panel, category);
                config.OnRoleplayAnimationSelected(panel, 1);
                Assert.AreEqual(
                    string.Empty,
                    InvokePrivateMethod(config, "GetPlayerCumTrigger"));
                config.OnPlayerCumReached(panel);
                AssertPrivateFieldString(config, "playbackMode", "Slow");
                Assert.AreEqual(SimulationState.Thrusting, panel.CurrentState);

                config.OnMaiOrgasm(panel);
                AssertPrivateFieldString(config, "playbackMode", "Cumming");
                Assert.AreEqual(SimulationState.Transitioning, panel.CurrentState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    [Test]
    public void SharedAccountingSeparatesBoobHandTongueAndMaiOrgasm()
    {
        var managerObject = new GameObject("Roleplay Paizuri accounting manager");
        FieldInfo instanceField = GetField(
            typeof(SexSimulationManager),
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousInstance = instanceField.GetValue(null);

        try
        {
            instanceField.SetValue(null, null);
            var manager = managerObject.AddComponent<SexSimulationManager>();
            var player = new Player("Roleplay Paizuri accounting player");
            SetSkillValue(player, SkillType.Size, 1000);
            SetSkillValue(player, SkillType.Cum, 30);
            SetSkillValue(player, SkillType.Hand, 20);
            SetSkillValue(player, SkillType.Tongue, 40);
            SetPrivateFieldValue(manager, "_cachedPlayer", player);
            manager.SetCurrentPosition(SexPositionConfigType.RoleplayPaizuri);

            int initialStamina = player.GetStamina();
            manager.StartRoleplayPaizuriPlayback(RoleplayCategory.Boob, false);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(3f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(0f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(1, manager.GetSessionData().boobsOperationCount);
            Assert.AreEqual(0, manager.GetSessionData().handOperationCount);

            manager.StartRoleplayPaizuriPlayback(RoleplayCategory.Hand, false);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(3f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(2f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(1, manager.GetSessionData().handOperationCount);

            manager.StartRoleplayPaizuriPlayback(RoleplayCategory.Tongue, true);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(3f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(6f, manager.GetMaiOrgasmBar(), 0.001f);
            Assert.AreEqual(1, manager.GetSessionData().mouthOperationCount);
            Assert.AreEqual(initialStamina - 4, player.GetStamina());

            int maiCumEvents = 0;
            int playerCumEvents = 0;
            manager.OnMaiOrgasmReached += () => maiCumEvents++;
            manager.OnPlayerCumReached += () => playerCumEvents++;
            SetSkillValue(player, SkillType.Hand, 10);
            SetPrivateFieldValue(manager, "playerCumBar", 0f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 99f);
            manager.StartRoleplayPaizuriPlayback(RoleplayCategory.Hand, false);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(0f, manager.GetPlayerCumBar(), 0.001f);
            Assert.AreEqual(1, maiCumEvents);
            Assert.AreEqual(0, playerCumEvents);
            Assert.AreEqual(1, manager.GetSessionData().roleplayPaizuriOrgasmCount);
            Assert.AreEqual(1, manager.GetSessionData().GetTotalRoleplayOrgasms());
            Assert.AreEqual(25, manager.GetSessionData().CalculateRoleplaySexPoints());
            Assert.AreEqual(25, DefaultSettings.RoleplayPaizuriSexPoints);

            SetPrivateFieldValue(manager, "playerCumBar", 99f);
            SetPrivateFieldValue(manager, "maiOrgasmBar", 0f);
            manager.StartRoleplayPaizuriPlayback(RoleplayCategory.Boob, false);
            manager.OnRoleplayLoopComplete();
            Assert.AreEqual(1, playerCumEvents);
            Assert.AreEqual(1, manager.GetSessionData().roleplayPaizuriOrgasmCount);
        }
        finally
        {
            instanceField.SetValue(null, previousInstance);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void HandOrTongueMaiCumCompletesTheOpeningAndStopHandsOffToMissionary()
    {
        var root = new GameObject("Roleplay Paizuri Mai-complete handoff");
        FieldInfo transitionInstanceField = GetField(
            typeof(TransitionPanel),
            "<Instance>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);
        object previousTransitionInstance = transitionInstanceField.GetValue(null);

        try
        {
            transitionInstanceField.SetValue(null, null);
            var panel = root.AddComponent<SimulationNavigationPanel>();
            var config = (RoleplayPaizuriPositionConfig)panel.ConfigureRoleplayTestBench(
                SexPositionConfigType.RoleplayPaizuri,
                null);
            SetPrivateFieldValue(panel, "isRoleplayOpeningActive", true);

            config.OnRoleplayCategorySelected(panel, RoleplayCategory.Hand);
            config.OnRoleplayAnimationSelected(panel, 1);
            AssertPrivateFieldValue(panel, "isRoleplayOpeningComplete", false);

            config.OnMaiOrgasm(panel);
            AssertPrivateFieldValue(panel, "isRoleplayOpeningComplete", true);
            Assert.AreEqual(SimulationState.Transitioning, panel.CurrentState);

            config.OnAfterCummingEntered(panel);
            Assert.AreEqual(SimulationState.AfterCumming, panel.CurrentState);
            InvokePrivateMethod(panel, "HandleRoleplayStopClicked");

            AssertPrivateFieldString(panel, "positionType", "Missionary");
            Assert.AreEqual(SimulationState.Idle, panel.CurrentState);
            AssertPrivateFieldValue(panel, "isRoleplayOpeningActive", false);
        }
        finally
        {
            transitionInstanceField.SetValue(null, previousTransitionInstance);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void OnlyUnlockedPaizuriIsChosenAndOnlyItsCategoryIsRandomized()
    {
        var sceneTypes = new[]
        {
            SexSceneType.RoleplayPussy,
            SexSceneType.RoleplayButthole,
            SexSceneType.Blowjob,
            SexSceneType.Paizuri
        };
        var previous = sceneTypes.ToDictionary(
            type => "SexSceneUnlocked_" + type,
            type =>
            {
                string key = "SexSceneUnlocked_" + type;
                return (exists: PlayerPrefs.HasKey(key), value: PlayerPrefs.GetInt(key));
            });
        var panelObject = new GameObject("Roleplay Paizuri randomized opener");

        try
        {
            SexSceneUnlockService.SetUnlocked(SexSceneType.RoleplayPussy, false);
            SexSceneUnlockService.SetUnlocked(SexSceneType.RoleplayButthole, false);
            SexSceneUnlockService.SetUnlocked(SexSceneType.Blowjob, false);
            SexSceneUnlockService.SetUnlocked(SexSceneType.Paizuri, true);

            SexPositionConfigType selected = (SexPositionConfigType)InvokePrivateMethod(
                typeof(SimulationNavigationPanel),
                "GetRandomUnlockedRoleplayPosition");
            Assert.AreEqual(SexPositionConfigType.RoleplayPaizuri, selected);

            var panel = panelObject.AddComponent<SimulationNavigationPanel>();
            InvokePrivateMethod(panel, "BeginRoleplayOpening");
            AssertPrivateFieldString(panel, "positionType", "RoleplayPaizuri");
            var config = (RoleplayPaizuriPositionConfig)GetPrivateFieldValue(
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
                    PlayerPrefs.SetInt(pair.Key, pair.Value.value);
                else
                    PlayerPrefs.DeleteKey(pair.Key);
            }
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void AnimationEventGateRejectsInactiveAndTransitioningPlayback()
    {
        MethodInfo method = typeof(RoleplayPaizuriAnimationEventReceiver).GetMethod(
            "IsPlaybackEventAllowed",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        Assert.IsFalse((bool)method.Invoke(null, new object[] { false, false }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { true, true }));
        Assert.IsTrue((bool)method.Invoke(null, new object[] { true, false }));
    }

    [Test]
    public void AnimatorControllerMatchesTheCompletePaizuriContract()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(controller);
        Assert.AreEqual(1, controller.layers.Length);
        Assert.AreEqual(9, controller.parameters.Length);

        var expectedParameters = new Dictionary<string, AnimatorControllerParameterType>
        {
            ["SlowLoop"] = AnimatorControllerParameterType.Bool,
            ["FastLoop"] = AnimatorControllerParameterType.Bool,
            ["SlowValue"] = AnimatorControllerParameterType.Float,
            ["FastValue"] = AnimatorControllerParameterType.Float,
            ["CumDick1"] = AnimatorControllerParameterType.Trigger,
            ["CumDick2"] = AnimatorControllerParameterType.Trigger,
            ["CumDick3"] = AnimatorControllerParameterType.Trigger,
            ["CumMai"] = AnimatorControllerParameterType.Trigger,
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
        Assert.AreEqual(11, stateMachine.states.Length);
        Assert.AreEqual(0, stateMachine.anyStateTransitions.Length);
        Assert.AreEqual("Paizuri Start", stateMachine.defaultState.name);
        Assert.IsTrue(stateMachine.behaviours.Any(behaviour =>
            behaviour.GetType().Name == "CubismFadeStateObserver"));
        AssertBlendTree(GetState(stateMachine, "Paizuri Slow"), "SlowValue", SlowClips);
        AssertBlendTree(GetState(stateMachine, "Paizuri Fast"), "FastValue", FastClips);

        AssertImmediate(stateMachine, "Paizuri Start", "Paizuri Slow",
            ("SlowLoop", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Paizuri Start", "Paizuri Fast",
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Paizuri Slow", "Paizuri Fast",
            ("SlowLoop", AnimatorConditionMode.IfNot),
            ("FastLoop", AnimatorConditionMode.If));
        AssertImmediate(stateMachine, "Paizuri Fast", "Paizuri Slow",
            ("FastLoop", AnimatorConditionMode.IfNot),
            ("SlowLoop", AnimatorConditionMode.If));

        foreach (string source in new[] { "Paizuri Slow", "Paizuri Fast" })
        {
            AssertImmediate(stateMachine, source, "Paizuri Cumming Dick 1",
                ("CumDick1", AnimatorConditionMode.If));
            AssertImmediate(stateMachine, source, "Paizuri Cumming Dick 2",
                ("CumDick2", AnimatorConditionMode.If));
            AssertImmediate(stateMachine, source, "Paizuri Cumming Dick 3",
                ("CumDick3", AnimatorConditionMode.If));
            AssertImmediate(stateMachine, source, "Paizuri Cumming Mai",
                ("CumMai", AnimatorConditionMode.If));
            AssertImmediate(stateMachine, source, "Paizuri Start",
                ("Stop", AnimatorConditionMode.If));
        }

        AssertAutomaticAtZero(
            stateMachine,
            "Paizuri Cumming Dick 1",
            "Paizuri Cumming Dick 1 - Loop");
        AssertAutomaticAtZero(
            stateMachine,
            "Paizuri Cumming Dick 2",
            "Paizuri Cumming Dick 2 - Loop");
        AssertAutomaticAtZero(
            stateMachine,
            "Paizuri Cumming Dick 3",
            "Paizuri Cumming Dick 3 - Loop");
        AssertAutomaticAtZero(
            stateMachine,
            "Paizuri Cumming Mai",
            "Paizuri Cumming Mai - After");

        foreach (string terminal in new[]
                 {
                     "Paizuri Cumming Dick 1 - Loop",
                     "Paizuri Cumming Dick 2 - Loop",
                     "Paizuri Cumming Dick 3 - Loop",
                     "Paizuri Cumming Mai - After"
                 })
        {
            AssertImmediate(stateMachine, terminal, "Paizuri Start",
                ("Stop", AnimatorConditionMode.If));
            Assert.AreEqual(
                1,
                GetState(stateMachine, terminal)
                    .behaviours.OfType<RoleplayPaizuriAfterCummingStateBehaviour>()
                    .Count(),
                terminal);
        }

        Assert.AreEqual(22, stateMachine.states.Sum(child => child.state.transitions.Length));
        Assert.AreEqual(
            1,
            GetState(stateMachine, "Paizuri Start")
                .behaviours.OfType<RoleplayPaizuriStartStateBehaviour>().Count());

        Dictionary<string, AnimationClip> clips = GetControllerClips(stateMachine);
        Assert.AreEqual(27, clips.Count);
        var activeClips = new HashSet<string>(SlowClips.Concat(FastClips));
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
                .Where(evt => evt.functionName == "OnPaizuriLoopComplete")
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
        Assert.IsNotNull(prefab.GetComponent<RoleplayPaizuriAnimationEventReceiver>());
    }

    [Test]
    public void GameSceneAndTestBenchExposeTheCompletePaizuriRouting()
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
            Transform model = menu.transform.Find("Live2D/Paizuri");
            Assert.IsNotNull(model);
            Live2DMotionController motionController =
                model.GetComponent<Live2DMotionController>();
            Assert.IsNotNull(motionController);
            Assert.IsNotNull(model.GetComponent<RoleplayPaizuriAnimationEventReceiver>());

            Live2DPanel live2DPanel =
                menu.transform.Find("Live2D").GetComponent<Live2DPanel>();
            Assert.IsNotNull(live2DPanel);
            var live2DPanelSo = new SerializedObject(live2DPanel);
            SerializedProperty binding =
                live2DPanelSo.FindProperty("roleplayPaizuriMotionController");
            Assert.IsNotNull(binding);
            Assert.AreSame(motionController, binding.objectReferenceValue);
            Assert.AreSame(
                motionController,
                live2DPanel.GetSimulationMotionController(
                    SexPositionConfigType.RoleplayPaizuri));

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
                menu.transform.Find("Simulation/Positions/Positions/Paizuri");
            Assert.IsNotNull(positionButton);
            Assert.IsNotNull(positionButton.GetComponent<Button>());
            Assert.IsNotNull(menu.GetComponentInChildren<SexSceneUnlockPanelController>(true));

            string testBenchSource = File.ReadAllText(
                "Assets/Scripts/Live2D/Editor/Live2DTestBenchWindow.cs");
            StringAssert.Contains("IsRoleplayPaizuriLike", testBenchSource);
            StringAssert.Contains("SexPositionConfigType.RoleplayPaizuri", testBenchSource);
            StringAssert.Contains("\"Player Cum 100%\"", testBenchSource);
            StringAssert.Contains("\"Mai Cum 100%\"", testBenchSource);
            StringAssert.Contains("bool playerCumAvailable", testBenchSource);
            StringAssert.Contains("!playerCumAvailable", testBenchSource);
            StringAssert.Contains("bool maiCumAvailable", testBenchSource);
            StringAssert.Contains("!maiCumAvailable", testBenchSource);
            StringAssert.Contains("Paizuri Cumming Dick 1 - Loop", testBenchSource);
            StringAssert.Contains("Paizuri Cumming Dick 2 - Loop", testBenchSource);
            StringAssert.Contains("Paizuri Cumming Dick 3 - Loop", testBenchSource);
            StringAssert.Contains("Paizuri Cumming Mai - After", testBenchSource);

            string unlockPanelSource = File.ReadAllText(
                "Assets/Scripts/UI/SexSceneUnlockPanelController.cs");
            StringAssert.Contains("HandleRoleplayPaizuriPositionClicked", unlockPanelSource);
            StringAssert.Contains("SexPositionConfigType.RoleplayPaizuri", unlockPanelSource);
        }
        finally
        {
            if (openedForTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void AssertCategory(
        RoleplayPaizuriPositionConfig config,
        SimulationNavigationPanel panel,
        RoleplayCategory category,
        IReadOnlyList<int> expectedBlendIndices,
        params Button[] animationButtons)
    {
        config.OnRoleplayCategorySelected(panel, category);
        Assert.IsFalse(config.AreRoleplayCategoriesAvailable());
        Assert.AreEqual(expectedBlendIndices.Count, config.GetRoleplayAnimationButtonCount());
        for (int index = 0; index < animationButtons.Length; index++)
        {
            Assert.AreEqual(index < expectedBlendIndices.Count, animationButtons[index].gameObject.activeSelf);
        }

        for (int animationNumber = 1;
             animationNumber <= expectedBlendIndices.Count;
             animationNumber++)
        {
            config.OnRoleplayAnimationSelected(panel, animationNumber);
            AssertPrivateFieldString(config, "playbackMode", "Slow");
            AssertPrivateFieldValue(
                config,
                "currentBlendIndex",
                expectedBlendIndices[animationNumber - 1]);
        }
    }

    private static void ReturnToStart(
        RoleplayPaizuriPositionConfig config,
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

    private static void AssertAutomaticAtZero(
        AnimatorStateMachine stateMachine,
        string sourceState,
        string destinationState)
    {
        AnimatorStateTransition transition =
            GetTransition(stateMachine, sourceState, destinationState);
        Assert.AreEqual(0, transition.conditions.Length);
        Assert.IsTrue(transition.hasExitTime);
        Assert.AreEqual(0f, transition.exitTime, 0.0001f);
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
