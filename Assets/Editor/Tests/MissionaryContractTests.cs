using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MaisLoveStory.Live2D;
using NUnit.Framework;
using UI.SexPosition;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionaryContractTests
{
    private const string ControllerPath =
        "Assets/Image/Bed Sex Scene/Sex - Missionary/Sex Missionary.controller";

    private const string MotionsFolder =
        "Assets/Image/Bed Sex Scene/Sex - Missionary/Motions";

    private const string PrefabPath =
        "Assets/Image/Bed Sex Scene/Sex - Missionary/Sex Missionary.prefab";

    private const string ScenePath = "Assets/Scenes/Game.unity";

    private const string TestBenchPath =
        "Assets/Scripts/Live2D/Editor/Live2DTestBenchWindow.cs";

    private static readonly string[] SkillTiers =
    {
        "Level 1-2",
        "Level 3-4",
        "Level 5"
    };

    private static readonly float[] SkillThresholds = { 1.5f, 3.5f, 5f };

    private static readonly HashSet<string> GameplayEventNames = new()
    {
        "OnThrust",
        "OnInsertComplete",
        "OnCumOutsideComplete",
        "OnPulloutComplete",
        "OnCumInsideComplete"
    };

    private static readonly HashSet<string> NonLoopingClips =
        BuildNonLoopingClipSet();

    [Test]
    public void AnimatorControllerMatchesOptimizedMissionaryContract()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(controller);
        Assert.AreEqual("8c7b8583a02741841b1d368774f36d2f",
            AssetDatabase.AssetPathToGUID(ControllerPath));
        Assert.AreEqual(1, controller.layers.Length);

        string[] expectedParameterOrder =
        {
            "IsButthole",
            "UseCondom",
            "Thrusting",
            "Fast",
            "SquirtingLoop",
            "SkillValue",
            "Insert",
            "Stop",
            "Cum",
            "CumOutside",
            "PullOut",
            "Waiting",
            "Squirting"
        };
        CollectionAssert.AreEqual(
            expectedParameterOrder,
            controller.parameters.Select(parameter => parameter.name).ToArray());

        foreach (string parameter in expectedParameterOrder.Take(5))
        {
            AssertParameter(
                controller,
                parameter,
                AnimatorControllerParameterType.Bool,
                expectedBool: false);
        }

        AssertParameter(
            controller,
            "SkillValue",
            AnimatorControllerParameterType.Float,
            expectedFloat: 1.5f);

        foreach (string parameter in expectedParameterOrder.Skip(6))
        {
            AssertParameter(
                controller,
                parameter,
                AnimatorControllerParameterType.Trigger);
        }

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        Assert.IsNotNull(root.defaultState);
        Assert.AreEqual("Missionary Start", root.defaultState.name);
        Assert.AreEqual(6, root.states.Length);
        Assert.AreEqual(2, root.stateMachines.Length);
        CollectionAssert.AreEquivalent(
            new[] { "Pussy", "Butthole" },
            root.stateMachines.Select(child => child.stateMachine.name).ToArray());
        Assert.AreEqual(
            1,
            root.behaviours.Count(behaviour =>
                behaviour != null &&
                behaviour.GetType().FullName ==
                "Live2D.Cubism.Framework.MotionFade.CubismFadeStateObserver"));

        AnimatorStateMachine pussy = GetStateMachine(root, "Pussy");
        AnimatorStateMachine butthole = GetStateMachine(root, "Butthole");
        Assert.AreEqual(17, pussy.states.Length);
        Assert.AreEqual(17, butthole.states.Length);
        Assert.AreEqual(0, pussy.anyStateTransitions.Length);
        Assert.AreEqual(0, butthole.anyStateTransitions.Length);

        Dictionary<string, AnimatorState> states = GetStates(root);
        Assert.AreEqual(40, states.Count);
        Assert.AreEqual(16, states.Values.Count(state => state.motion is AnimationClip));
        Assert.AreEqual(24, states.Values.Count(state => state.motion is BlendTree));
        Assert.IsFalse(states.Values.Any(state => state.motion == null));

        var clipOccurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, AnimatorState> pair in states)
        {
            if (pair.Value.motion is AnimationClip directClip)
            {
                Assert.AreEqual(GetDirectClipName(pair.Key), directClip.name);
                CountClip(clipOccurrences, directClip);
                continue;
            }

            var tree = pair.Value.motion as BlendTree;
            Assert.IsNotNull(tree, pair.Key);
            Assert.AreEqual(BlendTreeType.Simple1D, tree.blendType, pair.Key);
            Assert.AreEqual("SkillValue", tree.blendParameter, pair.Key);
            Assert.IsFalse(tree.useAutomaticThresholds, pair.Key);
            Assert.AreEqual(3, tree.children.Length, pair.Key);

            for (int index = 0; index < tree.children.Length; index++)
            {
                ChildMotion child = tree.children[index];
                Assert.AreEqual(
                    SkillThresholds[index],
                    child.threshold,
                    0.0001f,
                    $"{pair.Key} threshold {index}");
                Assert.IsInstanceOf<AnimationClip>(child.motion, pair.Key);
                Assert.AreEqual(
                    GetTierClipName(pair.Key, SkillTiers[index]),
                    child.motion.name,
                    $"{pair.Key} child {index}");
                CountClip(clipOccurrences, (AnimationClip)child.motion);
            }
        }

        Assert.AreEqual(88, clipOccurrences.Count);
        Assert.IsTrue(
            clipOccurrences.All(pair => pair.Value == 1),
            "Every Missionary clip must be assigned exactly once.");

        int transitionCount = root.anyStateTransitions.Length +
                              states.Values.Sum(state => state.transitions.Length);
        Assert.AreEqual(61, transitionCount);
        Assert.IsFalse(states.Values.Any(state =>
            state.transitions.Any(transition => transition.destinationState == state)));

        AssertSelectionTransitions(root, states);
        AssertAutomaticAndPlaybackTransitions(states);
    }

    [Test]
    public void AuthoredAndGeneratedClipsMatchLoopAndEventContract()
    {
        Dictionary<string, AnimationClip> clips = GetMissionaryClips();
        Assert.AreEqual(88, clips.Count);

        int callbackCount = 0;
        var callbackBreakdown = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            bool expectedLoop = !NonLoopingClips.Contains(pair.Key);
            bool generatedLoop =
                AnimationUtility.GetAnimationClipSettings(pair.Value).loopTime;
            Assert.AreEqual(expectedLoop, generatedLoop, pair.Key);

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(pair.Value);
            Assert.AreEqual(
                1,
                events.Count(animationEvent => animationEvent.functionName == "InstanceId"),
                pair.Key);

            string expectedCallback = GetExpectedGameplayCallback(pair.Key);
            AnimationEvent[] gameplayEvents = events
                .Where(animationEvent =>
                    GameplayEventNames.Contains(animationEvent.functionName))
                .ToArray();
            if (expectedCallback == null)
            {
                Assert.AreEqual(0, gameplayEvents.Length, pair.Key);
                continue;
            }

            Assert.AreEqual(1, gameplayEvents.Length, pair.Key);
            Assert.AreEqual(expectedCallback, gameplayEvents[0].functionName, pair.Key);
            Assert.AreEqual(pair.Value.length, gameplayEvents[0].time, 0.0001f, pair.Key);
            Assert.AreEqual(SendMessageOptions.RequireReceiver,
                gameplayEvents[0].messageOptions,
                pair.Key);
            callbackCount++;
            callbackBreakdown[expectedCallback] =
                callbackBreakdown.TryGetValue(expectedCallback, out int count)
                    ? count + 1
                    : 1;
        }

        Assert.AreEqual(52, callbackCount);
        Assert.AreEqual(24, callbackBreakdown["OnThrust"]);
        Assert.AreEqual(12, callbackBreakdown["OnInsertComplete"]);
        Assert.AreEqual(2, callbackBreakdown["OnCumOutsideComplete"]);
        Assert.AreEqual(2, callbackBreakdown["OnPulloutComplete"]);
        Assert.AreEqual(12, callbackBreakdown["OnCumInsideComplete"]);

        string[] motionJsonPaths = AssetDatabase.FindAssets(
                string.Empty,
                new[] { MotionsFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(
                ".motion3.json",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.AreEqual(88, motionJsonPaths.Length);

        var authoredNonLoops = new HashSet<string>(StringComparer.Ordinal);
        foreach (string motionPath in motionJsonPaths)
        {
            Match match = Regex.Match(
                File.ReadAllText(motionPath),
                "\\\"Loop\\\"\\s*:\\s*(true|false)",
                RegexOptions.IgnoreCase);
            Assert.IsTrue(match.Success, motionPath);
            if (bool.Parse(match.Groups[1].Value))
            {
                continue;
            }

            string withoutJson = Path.GetFileNameWithoutExtension(motionPath);
            authoredNonLoops.Add(Path.GetFileNameWithoutExtension(withoutJson));
        }

        CollectionAssert.AreEquivalent(NonLoopingClips, authoredNonLoops);
    }

    [Test]
    public void PrefabAndGameSceneHaveMissionaryRuntimeBindings()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab);
        Animator prefabAnimator = prefab.GetComponent<Animator>();
        Live2DMotionController prefabMotion =
            prefab.GetComponent<Live2DMotionController>();
        Live2D.MissionaryAnimationEventReceiver prefabReceiver =
            prefab.GetComponent<Live2D.MissionaryAnimationEventReceiver>();
        Assert.IsNotNull(prefabAnimator);
        Assert.IsNotNull(prefabMotion);
        Assert.IsNotNull(prefabReceiver);
        Assert.AreEqual(
            ControllerPath,
            AssetDatabase.GetAssetPath(prefabAnimator.runtimeAnimatorController));
        Assert.AreSame(
            prefabAnimator,
            new SerializedObject(prefabMotion)
                .FindProperty("animator")
                .objectReferenceValue);

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            GameObject menu = scene.GetRootGameObjects()
                .Single(rootObject => rootObject.name == "Menu");
            Transform live2D = menu.transform.Find("Live2D");
            Transform missionary = live2D.Find("Sex Missionary");
            Assert.IsNotNull(missionary);

            Live2DMotionController sceneMotion =
                missionary.GetComponent<Live2DMotionController>();
            Assert.IsNotNull(sceneMotion);
            Assert.IsNotNull(
                missionary.GetComponent<Live2D.MissionaryAnimationEventReceiver>());
            Assert.IsNotNull(missionary.GetComponent<Animator>());

            Live2DPanel panel = live2D.GetComponent<Live2DPanel>();
            Assert.IsNotNull(panel);
            SerializedProperty binding = new SerializedObject(panel)
                .FindProperty("missionaryMotionController");
            Assert.IsNotNull(binding);
            Assert.AreSame(sceneMotion, binding.objectReferenceValue);

            GameObject source =
                PrefabUtility.GetCorrespondingObjectFromSource(missionary.gameObject);
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
    }

    [Test]
    public void RuntimeConfigResetsQueuesAndDrivesConsolidatedParameters()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var modelObject = new GameObject("Missionary contract model");
        var panelObject = new GameObject("Missionary contract panel");
        try
        {
            Animator animator = modelObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            Live2DMotionController motionController =
                modelObject.AddComponent<Live2DMotionController>();
            SetPrivateField(motionController, "animator", animator);
            SimulationNavigationPanel panel =
                panelObject.AddComponent<SimulationNavigationPanel>();
            SetPrivateField(panel, "motionController", motionController);

            var config = new MissionaryPositionConfig();
            config.Initialize();
            Assert.AreEqual("Missionary", config.GetPositionName());
            Assert.AreEqual(SimulationState.Idle, config.GetInitialState());

            panel.QueuePendingThrust(true);
            config.OnPositionEntered(panel);
            Assert.IsFalse(GetPrivateBool(panel, "hasPendingThrust"));
            Assert.IsTrue(panel.isPussySelected);
            Assert.IsTrue(panel.isInsertAnimationComplete);
            Assert.IsFalse(animator.GetBool("IsButthole"));
            Assert.IsFalse(animator.GetBool("UseCondom"));
            Assert.IsFalse(animator.GetBool("Thrusting"));
            Assert.IsFalse(animator.GetBool("Fast"));
            Assert.IsFalse(animator.GetBool("SquirtingLoop"));
            Assert.AreEqual(
                panel.GetSkillAnimationValue(Base.Character.Skills.SkillType.F),
                animator.GetFloat("SkillValue"),
                0.0001f);

            config.OnHoleSelected(panel, HoleType.Pussy);
            Assert.AreEqual(SimulationState.Active, panel.CurrentState);
            Assert.IsFalse(panel.isInsertAnimationComplete);
            config.OnThrustStarted(panel, true);
            Assert.IsTrue(GetPrivateBool(panel, "hasPendingThrust"));
            Assert.IsFalse(animator.GetBool("Thrusting"));

            config.OnThrustStopped(panel);
            Assert.IsFalse(GetPrivateBool(panel, "hasPendingThrust"));
            Assert.AreEqual(SimulationState.Idle, panel.CurrentState);

            config.OnHoleSelected(panel, HoleType.Pussy);
            panel.isInsertAnimationComplete = true;
            config.OnThrustStarted(panel, true);
            Assert.AreEqual(SimulationState.Thrusting, panel.CurrentState);
            Assert.IsTrue(animator.GetBool("Thrusting"));
            Assert.IsTrue(animator.GetBool("Fast"));

            config.OnCumDecision(panel, CumDecisionType.Inside);
            Assert.AreEqual(SimulationState.Transitioning, panel.CurrentState);
            Assert.IsFalse(animator.GetBool("Thrusting"));
        }
        finally
        {
            SexSimulationManager.Instance?.StopThrusting();
            UnityEngine.Object.DestroyImmediate(panelObject);
            UnityEngine.Object.DestroyImmediate(modelObject);
        }
    }

    [Test]
    public void EventGateAndTestBenchUseMissionaryPlaybackContract()
    {
        MethodInfo gate = typeof(Live2D.MissionaryAnimationEventReceiver)
            .GetMethod(
                "IsPlaybackEventAllowed",
                BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(gate);
        Assert.IsFalse((bool)gate.Invoke(null, new object[] { false, false }));
        Assert.IsFalse((bool)gate.Invoke(null, new object[] { false, true }));
        Assert.IsFalse((bool)gate.Invoke(null, new object[] { true, true }));
        Assert.IsTrue((bool)gate.Invoke(null, new object[] { true, false }));

        string source = File.ReadAllText(TestBenchPath);
        foreach (string parameter in new[]
                 {
                     "IsButthole", "UseCondom", "Thrusting", "Fast",
                     "SquirtingLoop", "SkillValue", "Insert", "Stop",
                     "Cum", "CumOutside", "PullOut", "Waiting", "Squirting"
                 })
        {
            StringAssert.Contains($"\"{parameter}\"", source, parameter);
        }

        StringAssert.Contains("benchMissionarySkillTier", source);
        StringAssert.Contains("benchMissionaryUseCondom", source);
        StringAssert.DoesNotContain("PussySlowLoop", source);
        StringAssert.DoesNotContain("ButtholeSlowLoop", source);
        StringAssert.DoesNotContain("PussyInsertLoop", source);
        StringAssert.DoesNotContain("ButtholeInsertLoop", source);
    }

    private static void AssertSelectionTransitions(
        AnimatorStateMachine root,
        IReadOnlyDictionary<string, AnimatorState> states)
    {
        foreach ((string trigger, string suffix) in new[]
                 {
                     ("Insert", "Insert"),
                     ("Stop", "Stop"),
                     ("Cum", "Cum")
                 })
        {
            AssertAnyImmediate(root, states["Pussy " + suffix],
                (trigger, AnimatorConditionMode.If),
                ("IsButthole", AnimatorConditionMode.IfNot),
                ("UseCondom", AnimatorConditionMode.IfNot));
            AssertAnyImmediate(root, states["Pussy " + suffix + " CD"],
                (trigger, AnimatorConditionMode.If),
                ("IsButthole", AnimatorConditionMode.IfNot),
                ("UseCondom", AnimatorConditionMode.If));
            AssertAnyImmediate(root, states["Butthole " + suffix],
                (trigger, AnimatorConditionMode.If),
                ("IsButthole", AnimatorConditionMode.If),
                ("UseCondom", AnimatorConditionMode.IfNot));
            AssertAnyImmediate(root, states["Butthole " + suffix + " CD"],
                (trigger, AnimatorConditionMode.If),
                ("IsButthole", AnimatorConditionMode.If),
                ("UseCondom", AnimatorConditionMode.If));
        }

        AssertAnyImmediate(root, states["Pussy Cum Outside"],
            ("CumOutside", AnimatorConditionMode.If),
            ("IsButthole", AnimatorConditionMode.IfNot));
        AssertAnyImmediate(root, states["Butthole Cum Outside"],
            ("CumOutside", AnimatorConditionMode.If),
            ("IsButthole", AnimatorConditionMode.If));
        AssertAnyImmediate(root, states["Pussy Pull Out"],
            ("PullOut", AnimatorConditionMode.If),
            ("IsButthole", AnimatorConditionMode.IfNot));
        AssertAnyImmediate(root, states["Butthole Pull Out"],
            ("PullOut", AnimatorConditionMode.If),
            ("IsButthole", AnimatorConditionMode.If));
        AssertAnyImmediate(root, states["Missionary Squirting"],
            ("Squirting", AnimatorConditionMode.If));
    }

    private static void AssertAutomaticAndPlaybackTransitions(
        IReadOnlyDictionary<string, AnimatorState> states)
    {
        AssertAutomatic(states, "Missionary Start", "Missionary Standard - Loop");
        AssertAutomatic(states, "Missionary Waiting", "Missionary Standard - Loop");
        AssertImmediate(states, "Missionary Standard - Loop", "Missionary Waiting",
            ("Waiting", AnimatorConditionMode.If));
        AssertAutomatic(states, "Missionary Squirting", "Missionary Squirting - Loop");

        foreach (string hole in new[] { "Pussy", "Butthole" })
        {
            foreach (string cd in new[] { string.Empty, " CD" })
            {
                AssertAutomatic(states, hole + " Stop" + cd, "Missionary Standard - Loop");
                AssertAutomatic(
                    states,
                    hole + " Insert" + cd,
                    hole + " Insert" + cd + " - Loop");
                AssertAutomatic(
                    states,
                    hole + " Cum" + cd,
                    hole + " Cum" + cd + " - Loop");

                AssertImmediate(
                    states,
                    hole + " Insert" + cd + " - Loop",
                    hole + " Slow" + cd,
                    ("Thrusting", AnimatorConditionMode.If),
                    ("Fast", AnimatorConditionMode.IfNot));
                AssertImmediate(
                    states,
                    hole + " Insert" + cd + " - Loop",
                    hole + " Fast" + cd,
                    ("Thrusting", AnimatorConditionMode.If),
                    ("Fast", AnimatorConditionMode.If));
                AssertImmediate(
                    states,
                    hole + " Slow" + cd,
                    hole + " Fast" + cd,
                    ("Fast", AnimatorConditionMode.If));
                AssertImmediate(
                    states,
                    hole + " Fast" + cd,
                    hole + " Slow" + cd,
                    ("Fast", AnimatorConditionMode.IfNot));

                AnimatorConditionMode holeMode = hole == "Butthole"
                    ? AnimatorConditionMode.If
                    : AnimatorConditionMode.IfNot;
                AnimatorConditionMode condomMode = cd.Length > 0
                    ? AnimatorConditionMode.If
                    : AnimatorConditionMode.IfNot;
                AssertImmediate(
                    states,
                    "Missionary Squirting - Loop",
                    hole + " Slow" + cd,
                    ("SquirtingLoop", AnimatorConditionMode.IfNot),
                    ("Thrusting", AnimatorConditionMode.If),
                    ("Fast", AnimatorConditionMode.IfNot),
                    ("IsButthole", holeMode),
                    ("UseCondom", condomMode));
                AssertImmediate(
                    states,
                    "Missionary Squirting - Loop",
                    hole + " Fast" + cd,
                    ("SquirtingLoop", AnimatorConditionMode.IfNot),
                    ("Thrusting", AnimatorConditionMode.If),
                    ("Fast", AnimatorConditionMode.If),
                    ("IsButthole", holeMode),
                    ("UseCondom", condomMode));
            }

            AssertAutomatic(states, hole + " Cum Outside", "Cum Outside - Loop");
            AssertAutomatic(states, hole + " Pull Out", hole + " Pull Out - Loop");
        }
    }

    private static void AssertAnyImmediate(
        AnimatorStateMachine root,
        AnimatorState destination,
        params (string parameter, AnimatorConditionMode mode)[] conditions)
    {
        AnimatorStateTransition[] matches = root.anyStateTransitions
            .Where(transition => transition.destinationState == destination)
            .ToArray();
        Assert.AreEqual(1, matches.Length, "Any State -> " + destination.name);
        AssertImmediateTiming(matches[0], "Any State -> " + destination.name);
        AssertConditions(matches[0], conditions);
    }

    private static void AssertImmediate(
        IReadOnlyDictionary<string, AnimatorState> states,
        string source,
        string destination,
        params (string parameter, AnimatorConditionMode mode)[] conditions)
    {
        AnimatorStateTransition transition = GetTransition(states, source, destination);
        AssertImmediateTiming(transition, source + " -> " + destination);
        AssertConditions(transition, conditions);
    }

    private static void AssertAutomatic(
        IReadOnlyDictionary<string, AnimatorState> states,
        string source,
        string destination)
    {
        AnimatorStateTransition transition = GetTransition(states, source, destination);
        Assert.AreEqual(0, transition.conditions.Length, source + " -> " + destination);
        Assert.IsTrue(transition.hasExitTime, source + " -> " + destination);
        Assert.AreEqual(1f, transition.exitTime, 0.0001f, source + " -> " + destination);
        AssertTransitionTimingCommon(transition, source + " -> " + destination);
    }

    private static void AssertImmediateTiming(
        AnimatorStateTransition transition,
        string context)
    {
        Assert.IsFalse(transition.hasExitTime, context);
        Assert.AreEqual(0f, transition.exitTime, 0.0001f, context);
        AssertTransitionTimingCommon(transition, context);
    }

    private static void AssertTransitionTimingCommon(
        AnimatorStateTransition transition,
        string context)
    {
        Assert.IsTrue(transition.hasFixedDuration, context);
        Assert.AreEqual(0f, transition.duration, 0.0001f, context);
        Assert.AreEqual(0f, transition.offset, 0.0001f, context);
        Assert.AreEqual(TransitionInterruptionSource.None, transition.interruptionSource, context);
        Assert.IsFalse(transition.orderedInterruption, context);
        Assert.IsFalse(transition.canTransitionToSelf, context);
        Assert.IsFalse(transition.mute, context);
        Assert.IsFalse(transition.solo, context);
    }

    private static void AssertConditions(
        AnimatorStateTransition transition,
        params (string parameter, AnimatorConditionMode mode)[] expected)
    {
        Assert.AreEqual(expected.Length, transition.conditions.Length);
        foreach ((string parameter, AnimatorConditionMode mode) in expected)
        {
            AnimatorCondition[] matches = transition.conditions
                .Where(condition =>
                    condition.parameter == parameter && condition.mode == mode)
                .ToArray();
            Assert.AreEqual(1, matches.Length, $"Missing {mode} for {parameter}");
            Assert.AreEqual(0f, matches[0].threshold, 0.0001f, parameter);
        }
    }

    private static AnimatorStateTransition GetTransition(
        IReadOnlyDictionary<string, AnimatorState> states,
        string source,
        string destination)
    {
        AnimatorStateTransition[] matches = states[source].transitions
            .Where(transition => transition.destinationState?.name == destination)
            .ToArray();
        Assert.AreEqual(1, matches.Length, source + " -> " + destination);
        return matches[0];
    }

    private static Dictionary<string, AnimatorState> GetStates(
        AnimatorStateMachine root)
    {
        var states = new Dictionary<string, AnimatorState>(StringComparer.Ordinal);
        AddStates(root, states);
        return states;
    }

    private static void AddStates(
        AnimatorStateMachine machine,
        IDictionary<string, AnimatorState> states)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            Assert.IsFalse(states.ContainsKey(child.state.name), child.state.name);
            states.Add(child.state.name, child.state);
        }

        foreach (ChildAnimatorStateMachine child in machine.stateMachines)
        {
            AddStates(child.stateMachine, states);
        }
    }

    private static AnimatorStateMachine GetStateMachine(
        AnimatorStateMachine root,
        string name)
    {
        AnimatorStateMachine[] matches = root.stateMachines
            .Select(child => child.stateMachine)
            .Where(machine => machine.name == name)
            .ToArray();
        Assert.AreEqual(1, matches.Length, name);
        return matches[0];
    }

    private static Dictionary<string, AnimationClip> GetMissionaryClips()
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

    private static string GetExpectedGameplayCallback(string clipName)
    {
        if ((clipName.Contains(" Slow") || clipName.Contains(" Fast")) &&
            clipName.Contains("(Level"))
        {
            return "OnThrust";
        }

        if (clipName.Contains(" Insert") &&
            clipName.Contains("(Level") &&
            !clipName.Contains("Loop"))
        {
            return "OnInsertComplete";
        }

        if (clipName.EndsWith(" Cum Outside", StringComparison.Ordinal))
        {
            return "OnCumOutsideComplete";
        }

        if (clipName.EndsWith(" Pull Out", StringComparison.Ordinal))
        {
            return "OnPulloutComplete";
        }

        if (clipName.Contains(" Cum") &&
            clipName.Contains("(Level") &&
            !clipName.Contains("Loop"))
        {
            return "OnCumInsideComplete";
        }

        return null;
    }

    private static HashSet<string> BuildNonLoopingClipSet()
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "Missionary Start",
            "Missionary Waiting",
            "Missionary Squirting"
        };

        foreach (string hole in new[] { "Pussy", "Butthole" })
        {
            names.Add("Missionary " + hole + " Stop");
            names.Add("Missionary " + hole + " Stop CD");
            names.Add("Missionary " + hole + " Cum Outside");
            names.Add("Missionary " + hole + " Pull Out");
            foreach (string tier in SkillTiers)
            {
                names.Add($"Missionary {hole} Insert ({tier})");
                names.Add($"Missionary {hole} Insert CD ({tier})");
                names.Add($"Missionary {hole} Cum ({tier})");
                names.Add($"Missionary {hole} Cum CD ({tier})");
            }
        }

        Assert.AreEqual(35, names.Count);
        return names;
    }

    private static string GetTierClipName(string stateName, string tier)
    {
        return stateName switch
        {
            "Pussy Insert - Loop" => $"Missionary Pussy Insert ({tier}) - Loop",
            "Pussy Insert CD - Loop" => $"Missionary Pussy Insert CD ({tier}) - Loop",
            "Pussy Slow CD" => $"Missionary Pussy Slow - CD ({tier})",
            "Pussy Fast CD" => $"Missionary Pussy Fast - CD ({tier})",
            _ => $"Missionary {stateName} ({tier})"
        };
    }

    private static string GetDirectClipName(string stateName)
    {
        return stateName.StartsWith("Missionary ", StringComparison.Ordinal)
            ? stateName
            : "Missionary " + stateName;
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
        AnimatorControllerParameterType type,
        bool? expectedBool = null,
        float? expectedFloat = null)
    {
        AnimatorControllerParameter[] matches = controller.parameters
            .Where(parameter => parameter.name == name)
            .ToArray();
        Assert.AreEqual(1, matches.Length, name);
        Assert.AreEqual(type, matches[0].type, name);
        if (expectedBool.HasValue)
        {
            Assert.AreEqual(expectedBool.Value, matches[0].defaultBool, name);
        }

        if (expectedFloat.HasValue)
        {
            Assert.AreEqual(expectedFloat.Value, matches[0].defaultFloat, 0.0001f, name);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        field.SetValue(target, value);
    }

    private static bool GetPrivateBool(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        return (bool)field.GetValue(target);
    }
}
