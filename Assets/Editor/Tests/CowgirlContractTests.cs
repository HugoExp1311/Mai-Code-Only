using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Base;
using Base.Character;
using MaisLoveStory.Live2D;
using NUnit.Framework;
using UI.SexPosition;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CowgirlContractTests
{
    private const string ControllerPath =
        "Assets/Image/Bed Sex Scene/Sex - Cowgirl/Sex Cowgirl.controller";
    private const string MotionsFolder =
        "Assets/Image/Bed Sex Scene/Sex - Cowgirl/Motions";
    private const string PrefabPath =
        "Assets/Image/Bed Sex Scene/Sex - Cowgirl/Sex Cowgirl.prefab";
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string TestBenchPath =
        "Assets/Scripts/Live2D/Editor/Live2DTestBenchWindow.cs";
    private const string InventoryUiPath =
        "Assets/Scripts/Inventory/InventoryItemUI.cs";
    private const string UnlockPanelPath =
        "Assets/Scripts/UI/SexSceneUnlockPanelController.cs";

    private static readonly string[] Tiers =
    {
        "Level 1-2",
        "Level 3-4",
        "Level 5"
    };

    private static readonly float[] Thresholds = { 1.5f, 3.5f, 5f };
    private static readonly HashSet<string> NonLoopingClips =
        BuildNonLoopingClipSet();

    [Test]
    public void StableEnumsFactoryButtonsAndLewdLevelMappingMatchContract()
    {
        Assert.AreEqual(0, (int)SexPositionConfigType.Missionary);
        Assert.AreEqual(1, (int)SexPositionConfigType.Doggy);
        Assert.AreEqual(2, (int)SexPositionConfigType.Cowgirl);
        Assert.AreEqual(3, (int)SexPositionConfigType.Standing);
        Assert.AreEqual(4, (int)SexPositionConfigType.RoleplayPussy);
        Assert.AreEqual(5, (int)SexPositionConfigType.RoleplayButthole);
        Assert.AreEqual(6, (int)SexPositionConfigType.RoleplayBlowjob);
        Assert.AreEqual(7, (int)SexPositionConfigType.RoleplayPaizuri);

        SexPositionConfig created =
            SexPositionConfigFactory.Create(SexPositionConfigType.Cowgirl);
        Assert.IsInstanceOf<CowgirlPositionConfig>(created);
        created.Initialize();
        Assert.AreEqual("Cowgirl", created.GetPositionName());
        Assert.AreEqual(SimulationState.Idle, created.GetInitialState());

        ButtonVisibilitySet idle = created.GetButtonVisibility(SimulationState.Idle);
        Assert.IsTrue(idle.insertButton);
        Assert.IsTrue(idle.finishButton);
        ButtonVisibilitySet selecting =
            created.GetButtonVisibility(SimulationState.Selecting);
        Assert.IsTrue(selecting.pussyButton);
        Assert.IsFalse(selecting.buttholeButton);
        ButtonVisibilitySet active =
            created.GetButtonVisibility(SimulationState.Active);
        Assert.IsTrue(active.slowButton);
        Assert.IsTrue(active.fastButton);
        Assert.IsTrue(active.stopButton);
        Assert.IsTrue(active.finishButton);
        foreach (SimulationState state in Enum.GetValues(typeof(SimulationState)))
        {
            Assert.IsFalse(
                created.GetButtonVisibility(state).buttholeButton,
                state.ToString());
        }

        Assert.AreEqual(1.5f, CowgirlPositionConfig.GetLewdLevelValue(1));
        Assert.AreEqual(1.5f, CowgirlPositionConfig.GetLewdLevelValue(2));
        Assert.AreEqual(3.5f, CowgirlPositionConfig.GetLewdLevelValue(3));
        Assert.AreEqual(3.5f, CowgirlPositionConfig.GetLewdLevelValue(4));
        Assert.AreEqual(5f, CowgirlPositionConfig.GetLewdLevelValue(5));
    }

    [Test]
    public void WaitingTimerStartsAtIdleLoopAndAutoSlowUsesExactlyTwoLoops()
    {
        using var harness = new RuntimeHarness();
        CowgirlPositionConfig config = harness.Config;

        config.Tick(harness.Panel, 30f);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.Starting,
            config.PlaybackMode);

        config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.StandardLoopEntered);
        config.Tick(
            harness.Panel,
            CowgirlPositionConfig.InactivityDelaySeconds - 0.001f);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.Idle,
            config.PlaybackMode);

        config.Tick(harness.Panel, 0.001f);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.WaitingLeadIn,
            config.PlaybackMode);
        Assert.AreEqual(SimulationState.Idle, harness.Panel.CurrentState);
        Assert.IsTrue(
            config.GetButtonVisibility(harness.Panel.CurrentState).insertButton);

        config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.WaitingLoopEntered);
        Assert.AreEqual(SimulationState.Active, harness.Panel.CurrentState);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.WaitingLoop,
            config.PlaybackMode);

        config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.WaitingLoopCompleted);
        Assert.AreEqual(1, config.WaitingLoopsCompleted);
        Assert.IsFalse(harness.Manager.IsCowgirlPlaybackActive);

        config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.WaitingLoopCompleted);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.Thrusting,
            config.PlaybackMode);
        Assert.AreEqual(SimulationState.Thrusting, harness.Panel.CurrentState);
        Assert.IsTrue(harness.Manager.IsCowgirlPlaybackActive);
        Assert.IsFalse(harness.Animator.GetBool("Fast"));
    }

    [Test]
    public void WaitingCanBeInterruptedEarlyByManualFast()
    {
        using var harness = new RuntimeHarness();
        BeginWaitingLoop(harness);

        harness.Config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.WaitingLoopCompleted);
        harness.Config.OnThrustStarted(harness.Panel, true);
        harness.Config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.WaitingLoopCompleted);
        harness.Config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.WaitingLoopCompleted);

        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.Thrusting,
            harness.Config.PlaybackMode);
        Assert.IsTrue(harness.Animator.GetBool("Fast"));
        Assert.IsTrue(harness.Manager.IsCowgirlPlaybackActive);
    }

    [Test]
    public void CondomPersistsButAnExistingPenetrationKeepsItsSnapshot()
    {
        using var harness = new RuntimeHarness();
        EnterIdle(harness);
        BeginManualPenetration(harness);
        Assert.IsFalse(harness.Config.CurrentPenetrationUsesCondom);
        Assert.IsFalse(harness.Animator.GetBool("UseCondom"));

        Assert.IsTrue(harness.Manager.TryActivateCowgirlCondom());
        Assert.IsFalse(harness.Manager.TryActivateCowgirlCondom());
        Assert.IsTrue(harness.Manager.IsCowgirlCondomActive);
        Assert.IsFalse(harness.Config.CurrentPenetrationUsesCondom);
        Assert.IsFalse(harness.Animator.GetBool("UseCondom"));

        harness.Config.OnThrustStopped(harness.Panel);
        harness.Config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.StopLoopEntered);
        BeginManualPenetration(harness);
        Assert.IsTrue(harness.Config.CurrentPenetrationUsesCondom);
        Assert.IsTrue(harness.Animator.GetBool("UseCondom"));

        harness.Manager.SetCurrentPosition(SexPositionConfigType.Missionary);
        harness.Manager.SetCurrentPosition(SexPositionConfigType.Cowgirl);
        Assert.IsTrue(harness.Manager.IsCowgirlCondomActive);
        harness.Manager.ResetSimulation();
        Assert.IsFalse(harness.Manager.IsCowgirlCondomActive);

        string inventorySource = File.ReadAllText(InventoryUiPath);
        StringAssert.Contains("CanActivateCowgirlCondom", inventorySource);
        StringAssert.Contains("TryActivateCowgirlCondom", inventorySource);
        Assert.AreEqual(
            1,
            Regex.Matches(
                inventorySource,
                @"new\s+CharacterActions\.UseItem\(item\)").Count,
            "The accepted Cowgirl condom path must consume one item exactly once.");
    }

    [Test]
    public void ProtectedCumConsumesBulletWithoutPregnancyAccounting()
    {
        using var harness = new RuntimeHarness();
        SetPrivateField(harness.Manager, "currentBullets", 3);
        SexSessionData session = harness.Manager.GetSessionData();
        session.Reset();

        harness.Manager.OnCumInsideComplete(usedCondom: true);
        Assert.AreEqual(2, harness.Manager.GetCurrentBullets());
        Assert.AreEqual(0, session.cumInsideCount);
        Assert.AreEqual(0, session.GetPregnancyChance());

        harness.Manager.OnCumInsideComplete(usedCondom: false);
        Assert.AreEqual(1, harness.Manager.GetCurrentBullets());
        Assert.AreEqual(1, session.cumInsideCount);
        Assert.Greater(session.GetPregnancyChance(), 0);
    }

    [Test]
    public void SquirtingLoopPersistsUntilPlayerManuallyResumes()
    {
        using var harness = new RuntimeHarness();
        EnterIdle(harness);
        BeginManualPenetration(harness);
        harness.Panel.isInsertAnimationComplete = true;
        harness.Config.OnThrustStarted(harness.Panel, false);

        harness.Config.OnMaiOrgasm(harness.Panel);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.SquirtingLeadIn,
            harness.Config.PlaybackMode);
        Assert.IsFalse(harness.Manager.IsCowgirlPlaybackActive);

        harness.Config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.SquirtingLoopEntered);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.SquirtingLoop,
            harness.Config.PlaybackMode);
        Assert.AreEqual(SimulationState.Active, harness.Panel.CurrentState);
        Assert.IsFalse(harness.Manager.IsCowgirlPlaybackActive);

        harness.Config.OnThrustStarted(harness.Panel, true);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.Thrusting,
            harness.Config.PlaybackMode);
        Assert.IsTrue(harness.Manager.IsCowgirlPlaybackActive);
        Assert.IsTrue(harness.Animator.GetBool("Fast"));
    }

    [Test]
    public void SimultaneousBarsKeepCowgirlRewardButCumVisualWins()
    {
        using var harness = new RuntimeHarness();
        EnterIdle(harness);
        BeginManualPenetration(harness);
        harness.Panel.isInsertAnimationComplete = true;
        harness.Config.OnThrustStarted(harness.Panel, false);

        harness.Manager.OnPlayerCumReached +=
            () => harness.Config.OnPlayerCumReached(harness.Panel);
        harness.Manager.OnMaiOrgasmReached +=
            () => harness.Config.OnMaiOrgasm(harness.Panel);
        SetPrivateField(harness.Manager, "playerCumBar", 99.99f);
        SetPrivateField(harness.Manager, "maiOrgasmBar", 99.99f);

        harness.Manager.OnThrust();

        Assert.AreEqual(100f, harness.Manager.GetPlayerCumBar(), 0.0001f);
        Assert.AreEqual(
            CowgirlPositionConfig.CowgirlPlaybackMode.CumDecision,
            harness.Config.PlaybackMode);
        Assert.AreEqual(SimulationState.CumDecision, harness.Panel.CurrentState);
        Assert.AreEqual(1, harness.Manager.GetSessionData().cowgirlOrgasmCount);
    }

    [Test]
    public void AnimatorControllerAssignsAllMotionsAndBranchesExactlyOnce()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(controller);
        Assert.AreEqual(
            "b34bb87af7cb67b478a3def2c747e339",
            AssetDatabase.AssetPathToGUID(ControllerPath));
        Assert.AreEqual(1, controller.layers.Length);

        string[] parameterOrder =
        {
            "UseCondom", "Thrusting", "Fast", "LewdLevelValue",
            "Insert", "Stop", "Cum", "CumOutside", "PullOut",
            "Waiting", "Squirting"
        };
        CollectionAssert.AreEqual(
            parameterOrder,
            controller.parameters.Select(parameter => parameter.name).ToArray());
        foreach (string name in parameterOrder.Take(3))
        {
            AssertParameter(controller, name, AnimatorControllerParameterType.Bool);
        }
        AssertParameter(
            controller,
            "LewdLevelValue",
            AnimatorControllerParameterType.Float);
        foreach (string name in parameterOrder.Skip(4))
        {
            AssertParameter(controller, name, AnimatorControllerParameterType.Trigger);
        }

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        Assert.AreEqual("Cowgirl Start", machine.defaultState.name);
        Assert.AreEqual(
            1,
            machine.behaviours.Count(behaviour =>
                behaviour != null &&
                behaviour.GetType().FullName ==
                "Live2D.Cubism.Framework.MotionFade.CubismFadeStateObserver"));
        Dictionary<string, AnimatorState> states = machine.states.ToDictionary(
            child => child.state.name,
            child => child.state,
            StringComparer.Ordinal);
        Assert.AreEqual(30, states.Count);
        Assert.AreEqual(12, states.Values.Count(state => state.motion is BlendTree));
        Assert.IsFalse(states.Values.Any(state => state.motion == null));

        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, AnimatorState> pair in states)
        {
            if (pair.Value.motion is AnimationClip direct)
            {
                CountClip(occurrences, direct);
                continue;
            }

            var tree = pair.Value.motion as BlendTree;
            Assert.IsNotNull(tree, pair.Key);
            Assert.AreEqual(BlendTreeType.Simple1D, tree.blendType, pair.Key);
            Assert.AreEqual("LewdLevelValue", tree.blendParameter, pair.Key);
            Assert.IsFalse(tree.useAutomaticThresholds, pair.Key);
            Assert.AreEqual(3, tree.children.Length, pair.Key);
            for (int index = 0; index < tree.children.Length; index++)
            {
                ChildMotion child = tree.children[index];
                Assert.AreEqual(Thresholds[index], child.threshold, 0.0001f, pair.Key);
                Assert.AreEqual(
                    pair.Key + " (" + Tiers[index] + ")",
                    child.motion.name,
                    pair.Key);
                CountClip(occurrences, (AnimationClip)child.motion);
            }
        }
        Assert.AreEqual(54, occurrences.Count);
        Assert.IsTrue(occurrences.All(pair => pair.Value == 1));

        AssertAutomatic(states, "Cowgirl Start", "Cowgirl Standard - Loop");
        AssertAutomatic(states, "Cowgirl Waiting", "Cowgirl Waiting - Loop");
        AssertAutomatic(states, "Cowgirl Waiting CD", "Cowgirl Waiting CD - Loop");
        AssertAutomatic(states, "Cowgirl Insert", "Cowgirl Insert - Loop");
        AssertAutomatic(states, "Cowgirl Insert CD", "Cowgirl Insert CD - Loop");
        AssertAutomatic(states, "Cowgirl Stop", "Cowgirl Stop - Loop");
        AssertAutomatic(states, "Cowgirl Stop CD", "Cowgirl Stop - Loop");
        AssertAutomatic(states, "Cowgirl Squirting", "Cowgirl Squirting - Loop");
        AssertAutomatic(states, "Cowgirl Squirting CD", "Cowgirl Squirting - Loop");
        AssertAutomatic(states, "Cowgirl Cum", "Cowgirl Cum - Loop");
        AssertAutomatic(states, "Cowgirl Cum CD", "Cowgirl Cum CD - Loop");
        AssertAutomatic(states, "Cowgirl Cum Outside", "Cowgirl Cum Outside - Loop");
        AssertAutomatic(states, "Cowgirl Pull Out", "Cowgirl Pull Out - Loop");
        AssertAutomatic(states, "Cowgirl Pull Out CD", "Cowgirl Pull Out CD - Loop");

        AssertAny(machine, states["Cowgirl Insert"],
            ("Insert", AnimatorConditionMode.If),
            ("UseCondom", AnimatorConditionMode.IfNot));
        AssertAny(machine, states["Cowgirl Insert CD"],
            ("Insert", AnimatorConditionMode.If),
            ("UseCondom", AnimatorConditionMode.If));
        AssertAny(machine, states["Cowgirl Cum Outside"],
            ("CumOutside", AnimatorConditionMode.If),
            ("UseCondom", AnimatorConditionMode.IfNot));
        AssertAny(machine, states["Cowgirl Slow"],
            ("Thrusting", AnimatorConditionMode.If),
            ("Fast", AnimatorConditionMode.IfNot),
            ("UseCondom", AnimatorConditionMode.IfNot));
        AssertAny(machine, states["Cowgirl Fast CD"],
            ("Thrusting", AnimatorConditionMode.If),
            ("Fast", AnimatorConditionMode.If),
            ("UseCondom", AnimatorConditionMode.If));

        var expectedSignals = new Dictionary<string, CowgirlAnimationSignal>
        {
            ["Cowgirl Standard - Loop"] = CowgirlAnimationSignal.StandardLoopEntered,
            ["Cowgirl Insert - Loop"] = CowgirlAnimationSignal.InsertLoopEntered,
            ["Cowgirl Insert CD - Loop"] = CowgirlAnimationSignal.InsertLoopEntered,
            ["Cowgirl Stop - Loop"] = CowgirlAnimationSignal.StopLoopEntered,
            ["Cowgirl Waiting - Loop"] = CowgirlAnimationSignal.WaitingLoopEntered,
            ["Cowgirl Waiting CD - Loop"] = CowgirlAnimationSignal.WaitingLoopEntered,
            ["Cowgirl Squirting - Loop"] = CowgirlAnimationSignal.SquirtingLoopEntered,
            ["Cowgirl Cum - Loop"] = CowgirlAnimationSignal.CumLoopEntered,
            ["Cowgirl Cum CD - Loop"] = CowgirlAnimationSignal.CumLoopEntered,
            ["Cowgirl Cum Outside - Loop"] = CowgirlAnimationSignal.CumOutsideLoopEntered,
            ["Cowgirl Pull Out - Loop"] = CowgirlAnimationSignal.PullOutLoopEntered,
            ["Cowgirl Pull Out CD - Loop"] = CowgirlAnimationSignal.PullOutLoopEntered
        };
        foreach (KeyValuePair<string, CowgirlAnimationSignal> pair in expectedSignals)
        {
            CowgirlStateEntryBehaviour[] behaviours = states[pair.Key].behaviours
                .OfType<CowgirlStateEntryBehaviour>()
                .ToArray();
            Assert.AreEqual(1, behaviours.Length, pair.Key);
            Assert.AreEqual(pair.Value, behaviours[0].Signal, pair.Key);
        }
    }

    [Test]
    public void AuthoredAndGeneratedMotionsMatchLoopAndEventContract()
    {
        Dictionary<string, AnimationClip> clips = GetClips();
        Assert.AreEqual(54, clips.Count);
        int thrustCallbacks = 0;
        int waitingCallbacks = 0;

        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            Assert.AreEqual(
                !NonLoopingClips.Contains(pair.Key),
                AnimationUtility.GetAnimationClipSettings(pair.Value).loopTime,
                pair.Key);
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(pair.Value);
            Assert.AreEqual(
                1,
                events.Count(animationEvent => animationEvent.functionName == "InstanceId"),
                pair.Key);

            bool thrustClip = pair.Key.StartsWith("Cowgirl Slow", StringComparison.Ordinal) ||
                              pair.Key.StartsWith("Cowgirl Fast", StringComparison.Ordinal);
            int thrustCount = events.Count(animationEvent => animationEvent.functionName == "OnThrust");
            Assert.AreEqual(thrustClip ? 1 : 0, thrustCount, pair.Key);
            thrustCallbacks += thrustCount;

            bool waitingLoop = pair.Key == "Cowgirl Waiting - Loop" ||
                               pair.Key == "Cowgirl Waiting CD - Loop";
            int waitingCount = events.Count(animationEvent =>
                animationEvent.functionName == "OnWaitingLoopComplete");
            Assert.AreEqual(waitingLoop ? 1 : 0, waitingCount, pair.Key);
            waitingCallbacks += waitingCount;
        }

        Assert.AreEqual(12, thrustCallbacks);
        Assert.AreEqual(2, waitingCallbacks);

        string[] jsonPaths = AssetDatabase.FindAssets(string.Empty, new[] { MotionsFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".motion3.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.AreEqual(54, jsonPaths.Length);
        var authoredNonLoops = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in jsonPaths)
        {
            Match match = Regex.Match(
                File.ReadAllText(path),
                "\\\"Loop\\\"\\s*:\\s*(true|false)",
                RegexOptions.IgnoreCase);
            Assert.IsTrue(match.Success, path);
            if (!bool.Parse(match.Groups[1].Value))
            {
                string withoutJson = Path.GetFileNameWithoutExtension(path);
                authoredNonLoops.Add(Path.GetFileNameWithoutExtension(withoutJson));
            }
        }
        CollectionAssert.AreEquivalent(NonLoopingClips, authoredNonLoops);
    }

    [Test]
    public void PrefabSceneUnlockVisibilityAndTestBenchBindingsAreComplete()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);
        Animator animator = prefab.GetComponent<Animator>();
        Live2DMotionController motion = prefab.GetComponent<Live2DMotionController>();
        Assert.IsNotNull(animator);
        Assert.IsNotNull(motion);
        Assert.IsNotNull(prefab.GetComponent<CowgirlAnimationEventReceiver>());
        Assert.AreEqual(
            ControllerPath,
            AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
        SerializedObject serializedMotion = new SerializedObject(motion);
        Assert.AreSame(
            animator,
            serializedMotion.FindProperty("animator").objectReferenceValue);

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            Transform live2D = scene.GetRootGameObjects()
                .Single(root => root.name == "Menu")
                .transform.Find("Live2D");
            Transform cowgirl = live2D.Find("Sex Cowgirl");
            Assert.IsNotNull(cowgirl);
            Assert.AreEqual(Vector3.zero, cowgirl.localPosition);
            Assert.AreEqual(new Vector3(1.31f, 1.31f, 1.31f), cowgirl.localScale);
            Live2DMotionController sceneMotion =
                cowgirl.GetComponent<Live2DMotionController>();
            Assert.IsNotNull(sceneMotion);
            Assert.IsNotNull(cowgirl.GetComponent<CowgirlAnimationEventReceiver>());

            Live2DPanel panel = live2D.GetComponent<Live2DPanel>();
            Assert.IsNotNull(panel);
            Assert.AreSame(sceneMotion, panel.CowgirlMotionController);
            Assert.AreSame(
                sceneMotion,
                panel.GetSimulationMotionController(SexPositionConfigType.Cowgirl));
            GameObject source =
                PrefabUtility.GetCorrespondingObjectFromSource(cowgirl.gameObject);
            Assert.IsNotNull(source);
            Assert.AreEqual(PrefabPath, AssetDatabase.GetAssetPath(source));
        }
        finally
        {
            if (openedForTest)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        string unlockSource = File.ReadAllText(UnlockPanelPath);
        StringAssert.Contains("HandleCowgirlPositionClicked", unlockSource);
        StringAssert.Contains("SexPositionConfigType.Cowgirl", unlockSource);

        string benchSource = File.ReadAllText(TestBenchPath);
        foreach (string parameter in new[]
                 {
                     "UseCondom", "LewdLevelValue", "Insert", "Thrusting",
                     "Fast", "Stop", "Waiting", "Squirting", "Cum",
                     "CumOutside", "PullOut"
                 })
        {
            StringAssert.Contains("\"" + parameter + "\"", benchSource, parameter);
        }
        StringAssert.Contains("IsCowgirlLike", benchSource);
        StringAssert.Contains("DrawCowgirlTransitions", benchSource);

        string live2DSource = File.ReadAllText("Assets/Scripts/UI/Live2DPanel.cs");
        StringAssert.Contains("HideAllSimulationLive2DExcept", live2DSource);
    }

    private static void EnterIdle(RuntimeHarness harness)
    {
        harness.Config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.StandardLoopEntered);
    }

    private static void BeginManualPenetration(RuntimeHarness harness)
    {
        harness.Config.OnInsertClicked(harness.Panel);
        harness.Config.OnHoleSelected(harness.Panel, HoleType.Pussy);
    }

    private static void BeginWaitingLoop(RuntimeHarness harness)
    {
        EnterIdle(harness);
        harness.Config.Tick(
            harness.Panel,
            CowgirlPositionConfig.InactivityDelaySeconds);
        harness.Config.OnAnimationSignal(
            harness.Panel,
            CowgirlAnimationSignal.WaitingLoopEntered);
    }

    private static Dictionary<string, AnimationClip> GetClips()
    {
        var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets(
                     "t:AnimationClip",
                     new[] { MotionsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                clips.Add(clip.name, clip);
            }
        }

        return clips;
    }

    private static HashSet<string> BuildNonLoopingClipSet()
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "Cowgirl Start",
            "Cowgirl Waiting",
            "Cowgirl Waiting CD",
            "Cowgirl Cum Outside",
            "Cowgirl Pull Out",
            "Cowgirl Pull Out CD",
            "Cowgirl Squirting",
            "Cowgirl Squirting CD",
            "Cowgirl Stop",
            "Cowgirl Stop CD"
        };

        foreach (string tier in Tiers)
        {
            names.Add("Cowgirl Cum (" + tier + ")");
            names.Add("Cowgirl Cum CD (" + tier + ")");
            names.Add("Cowgirl Insert (" + tier + ")");
            names.Add("Cowgirl Insert CD (" + tier + ")");
        }

        Assert.AreEqual(22, names.Count);
        return names;
    }

    private static void CountClip(
        IDictionary<string, int> occurrences,
        AnimationClip clip)
    {
        occurrences[clip.name] = occurrences.TryGetValue(clip.name, out int count)
            ? count + 1
            : 1;
    }

    private static void AssertParameter(
        AnimatorController controller,
        string name,
        AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter[] matches = controller.parameters
            .Where(parameter => parameter.name == name)
            .ToArray();
        Assert.AreEqual(1, matches.Length, name);
        Assert.AreEqual(type, matches[0].type, name);
    }

    private static void AssertAutomatic(
        IReadOnlyDictionary<string, AnimatorState> states,
        string source,
        string destination)
    {
        AnimatorStateTransition[] matches = states[source].transitions
            .Where(transition => transition.destinationState?.name == destination)
            .ToArray();
        Assert.AreEqual(1, matches.Length, source + " -> " + destination);
        AnimatorStateTransition transition = matches[0];
        Assert.AreEqual(0, transition.conditions.Length);
        Assert.IsTrue(transition.hasExitTime);
        Assert.AreEqual(1f, transition.exitTime, 0.0001f);
        AssertTransitionTiming(transition);
    }

    private static void AssertAny(
        AnimatorStateMachine machine,
        AnimatorState destination,
        params (string parameter, AnimatorConditionMode mode)[] expected)
    {
        AnimatorStateTransition[] matches = machine.anyStateTransitions
            .Where(transition => transition.destinationState == destination)
            .ToArray();
        Assert.AreEqual(1, matches.Length, "Any State -> " + destination.name);
        AnimatorStateTransition transition = matches[0];
        Assert.IsFalse(transition.hasExitTime);
        AssertTransitionTiming(transition);
        Assert.AreEqual(expected.Length, transition.conditions.Length);
        foreach ((string parameter, AnimatorConditionMode mode) in expected)
        {
            Assert.AreEqual(
                1,
                transition.conditions.Count(condition =>
                    condition.parameter == parameter && condition.mode == mode),
                parameter);
        }
    }

    private static void AssertTransitionTiming(AnimatorStateTransition transition)
    {
        Assert.IsTrue(transition.hasFixedDuration);
        Assert.AreEqual(0f, transition.duration, 0.0001f);
        Assert.AreEqual(0f, transition.offset, 0.0001f);
        Assert.AreEqual(TransitionInterruptionSource.None, transition.interruptionSource);
        Assert.IsFalse(transition.orderedInterruption);
        Assert.IsFalse(transition.canTransitionToSelf);
        Assert.IsFalse(transition.mute);
        Assert.IsFalse(transition.solo);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        field.SetValue(target, value);
    }

    private sealed class RuntimeHarness : IDisposable
    {
        private readonly FieldInfo instanceField;
        private readonly SexSimulationManager previousInstance;
        private readonly GameObject managerObject;
        private readonly GameObject modelObject;
        private readonly GameObject panelObject;

        public RuntimeHarness()
        {
            instanceField = typeof(SexSimulationManager).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(instanceField);
            previousInstance = instanceField.GetValue(null) as SexSimulationManager;
            instanceField.SetValue(null, null);

            managerObject = new GameObject("Cowgirl contract manager");
            Manager = managerObject.AddComponent<SexSimulationManager>();
            // EditMode AddComponent does not guarantee MonoBehaviour.Awake, so bind
            // the singleton explicitly for config/event paths under test.
            instanceField.SetValue(null, Manager);

            modelObject = new GameObject("Cowgirl contract model");
            Animator = modelObject.AddComponent<Animator>();
            Animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Live2DMotionController motion =
                modelObject.AddComponent<Live2DMotionController>();
            SetPrivateField(motion, "animator", Animator);

            panelObject = new GameObject("Cowgirl contract panel");
            Panel = panelObject.AddComponent<SimulationNavigationPanel>();
            SetPrivateField(Panel, "motionController", motion);

            Manager.SetCurrentPosition(SexPositionConfigType.Cowgirl);
            SetPrivateField(Manager, "_cachedPlayer", new Player("Cowgirl test"));
            SetPrivateField(Manager, "_cachedMai", new Target("Mai"));
            SetPrivateField(Manager, "currentBullets", 3);

            Config = new CowgirlPositionConfig();
            Config.Initialize();
            Config.OnPositionEntered(Panel);
            Panel.SetState(Config.GetInitialState());

            Manager.OnInsertAnimationComplete += () =>
                Panel.isInsertAnimationComplete = true;
            Manager.OnCumInsideAnimationComplete += () =>
                Panel.SetState(SimulationState.CumInside);
        }

        public SexSimulationManager Manager { get; }
        public Animator Animator { get; }
        public SimulationNavigationPanel Panel { get; }
        public CowgirlPositionConfig Config { get; }

        public void Dispose()
        {
            Manager.StopThrusting();
            UnityEngine.Object.DestroyImmediate(panelObject);
            UnityEngine.Object.DestroyImmediate(modelObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
            instanceField.SetValue(null, previousInstance);
        }
    }
}
