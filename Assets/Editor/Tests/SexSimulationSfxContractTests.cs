using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Base;
using FMODUnity;
using MaisLoveStory.Live2D;
using NUnit.Framework;
using UI.SexPosition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SexSimulationSfxContractTests
{
    private const string MissionaryControllerPath =
        "Assets/Image/Bed Sex Scene/Sex - Missionary/Sex Missionary.controller";
    private const string CowgirlControllerPath =
        "Assets/Image/Bed Sex Scene/Sex - Cowgirl/Sex Cowgirl.controller";
    private const string MissionaryPrefabPath =
        "Assets/Image/Bed Sex Scene/Sex - Missionary/Sex Missionary.prefab";
    private const string CowgirlPrefabPath =
        "Assets/Image/Bed Sex Scene/Sex - Cowgirl/Sex Cowgirl.prefab";
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string TestBenchPath =
        "Assets/Scripts/Live2D/Editor/Live2DTestBenchWindow.cs";
    private const string MissionaryReceiverPath =
        "Assets/Scripts/Live2D/MissionaryAnimationEventReceiver.cs";
    private const string CowgirlReceiverPath =
        "Assets/Scripts/Live2D/CowgirlAnimationEventReceiver.cs";

    private static readonly string[] RoleplayControllerPaths =
    {
        "Assets/Image/Bed Sex Scene/Roleplay - Pussy/Roleplay Pussy.controller",
        "Assets/Image/Bed Sex Scene/Roleplay - Butthole/Roleplay Butthole.controller",
        "Assets/Image/Bed Sex Scene/Roleplay - Blowjob/Roleplay Blowjob.controller",
        "Assets/Image/Bed Sex Scene/Roleplay - Paizuri/Paizuri.controller"
    };

    [Test]
    public void FmodEventsAreOneShot2DSfxAndGameSceneBindingsMatch()
    {
        var expectations = new Dictionary<string, Func<FMODEvents, EventReference>>
        {
            ["event:/SFX/Sex/Slow"] = events => events.SexSlow,
            ["event:/SFX/Sex/Fast"] = events => events.SexFast,
            ["event:/SFX/Sex/CumInside"] = events => events.SexCumInside,
            ["event:/SFX/Sex/Squirt"] = events => events.SexSquirt
        };

        foreach (string path in expectations.Keys)
        {
            EditorEventRef editorEvent = EventManager.EventFromPath(path);
            Assert.IsNotNull(editorEvent, path);
            Assert.IsTrue(editorEvent.IsOneShot, path);
            Assert.IsFalse(editorEvent.Is3D, path);
            Assert.IsTrue(editorEvent.Banks.Any(bank => bank.Name == "SFX"), path);
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            FMODEvents sceneEvents = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<FMODEvents>(true))
                .Single();
            foreach (KeyValuePair<string, Func<FMODEvents, EventReference>> expectation
                     in expectations)
            {
                EventReference binding = expectation.Value(sceneEvents);
                EditorEventRef editorEvent = EventManager.EventFromPath(expectation.Key);
                Assert.IsFalse(binding.IsNull, expectation.Key);
                Assert.AreEqual(expectation.Key, binding.Path);
                Assert.AreEqual(editorEvent.Guid, binding.Guid, expectation.Key);
            }
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
    public void PhysicalClipsCarryExactSfxCuesAndPreserveGameplayCallbacks()
    {
        int cumCueCount = 0;
        int squirtCueCount = 0;
        int fastCumCueCount = 0;
        int missionaryThrustCount = 0;
        int cowgirlThrustCount = 0;

        AnimationClip missionaryFastReference = GetUniqueClips(MissionaryControllerPath)
            .Single(clip => clip.name == "Missionary Pussy Fast (Level 1-2)");
        AnimationClip cowgirlFastReference = GetUniqueClips(CowgirlControllerPath)
            .Single(clip => clip.name == "Cowgirl Fast (Level 1-2)");
        float missionaryFastCueValue = GetFastCueValue(
            missionaryFastReference,
            true);
        float cowgirlFastCueValue = GetFastCueValue(
            cowgirlFastReference,
            false);

        foreach (string controllerPath in new[]
                 {
                     MissionaryControllerPath,
                     CowgirlControllerPath
                 })
        {
            bool missionary = controllerPath == MissionaryControllerPath;
            foreach (AnimationClip clip in GetUniqueClips(controllerPath))
            {
                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                Assert.AreEqual(
                    1,
                    events.Count(animationEvent =>
                        animationEvent.functionName == "InstanceId"),
                    clip.name);

                bool cumTarget = IsCumTarget(clip.name, missionary);
                AnimationEvent[] cumEvents = events
                    .Where(animationEvent =>
                        animationEvent.functionName == "OnCumInsideSfx")
                    .ToArray();
                Assert.AreEqual(cumTarget ? 1 : 0, cumEvents.Length, clip.name);
                if (cumTarget)
                {
                    AssertCue(cumEvents[0], 7.3f, clip.name);
                    cumCueCount++;
                    if (missionary)
                    {
                        AnimationEvent completion = events.Single(animationEvent =>
                            animationEvent.functionName == "OnCumInsideComplete");
                        Assert.AreEqual(clip.length, completion.time, 0.0001f, clip.name);
                    }
                }

                AnimationEvent[] fastCumEvents = events
                    .Where(animationEvent =>
                        animationEvent.functionName == "OnFastSfx")
                    .OrderBy(animationEvent => animationEvent.time)
                    .ToArray();
                if (cumTarget)
                {
                    float[] expectedTimes = missionary
                        ? GetMissionaryFastCueTimes(clip, missionaryFastCueValue)
                        : GetCowgirlFastCueTimes(clip, cowgirlFastCueValue);
                    int expectedCount = missionary ||
                                        clip.name.StartsWith(
                                            "Cowgirl Cum CD",
                                            StringComparison.Ordinal)
                        ? 25
                        : 27;
                    Assert.AreEqual(expectedCount, expectedTimes.Length, clip.name);
                    Assert.AreEqual(expectedTimes.Length, fastCumEvents.Length, clip.name);
                    for (int index = 0; index < expectedTimes.Length; index++)
                    {
                        AssertCue(fastCumEvents[index], expectedTimes[index], clip.name);
                        Assert.Greater(fastCumEvents[index].time, 0f, clip.name);
                        Assert.LessOrEqual(fastCumEvents[index].time, 7.0001f, clip.name);
                    }

                    fastCumCueCount += fastCumEvents.Length;
                }
                else
                {
                    Assert.AreEqual(0, fastCumEvents.Length, clip.name);
                }

                bool squirtTarget = IsSquirtTarget(clip.name, missionary);
                AnimationEvent[] squirtEvents = events
                    .Where(animationEvent =>
                        animationEvent.functionName == "OnSquirtSfx")
                    .ToArray();
                Assert.AreEqual(squirtTarget ? 1 : 0, squirtEvents.Length, clip.name);
                if (squirtTarget)
                {
                    AssertCue(
                        squirtEvents[0],
                        missionary ? 1.167f : 2.667f,
                        clip.name);
                    squirtCueCount++;
                }

                bool thrustTarget = missionary
                    ? (clip.name.Contains(" Slow") || clip.name.Contains(" Fast")) &&
                      clip.name.Contains("(Level")
                    : clip.name.StartsWith("Cowgirl Slow", StringComparison.Ordinal) ||
                      clip.name.StartsWith("Cowgirl Fast", StringComparison.Ordinal);
                int thrustEvents = events.Count(animationEvent =>
                    animationEvent.functionName == "OnThrust");
                Assert.AreEqual(thrustTarget ? 1 : 0, thrustEvents, clip.name);
                if (thrustTarget)
                {
                    if (missionary)
                    {
                        missionaryThrustCount++;
                    }
                    else
                    {
                        cowgirlThrustCount++;
                    }
                }
            }
        }

        Assert.AreEqual(18, cumCueCount);
        Assert.AreEqual(3, squirtCueCount);
        Assert.AreEqual(456, fastCumCueCount);
        Assert.AreEqual(24, missionaryThrustCount);
        Assert.AreEqual(12, cowgirlThrustCount);

        foreach (string controllerPath in RoleplayControllerPaths)
        {
            foreach (AnimationClip clip in GetUniqueClips(controllerPath))
            {
                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                Assert.AreEqual(
                    0,
                    events.Count(animationEvent =>
                        animationEvent.functionName == "OnCumInsideSfx" ||
                        animationEvent.functionName == "OnSquirtSfx" ||
                        animationEvent.functionName == "OnFastSfx"),
                    clip.name);
            }
        }
    }

    [Test]
    public void PhysicalPrefabsExposeBothSfxAnimationEventReceivers()
    {
        GameObject missionaryPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(MissionaryPrefabPath);
        GameObject cowgirlPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(CowgirlPrefabPath);
        Assert.IsNotNull(missionaryPrefab);
        Assert.IsNotNull(cowgirlPrefab);

        Live2D.MissionaryAnimationEventReceiver missionaryReceiver =
            missionaryPrefab.GetComponent<Live2D.MissionaryAnimationEventReceiver>();
        CowgirlAnimationEventReceiver cowgirlReceiver =
            cowgirlPrefab.GetComponent<CowgirlAnimationEventReceiver>();
        Assert.IsNotNull(missionaryReceiver);
        Assert.IsNotNull(cowgirlReceiver);

        foreach (Type receiverType in new[]
                 {
                     missionaryReceiver.GetType(),
                     cowgirlReceiver.GetType()
                 })
        {
            Assert.IsNotNull(receiverType.GetMethod(
                "OnCumInsideSfx",
                BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(receiverType.GetMethod(
                "OnSquirtSfx",
                BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(receiverType.GetMethod(
                "OnFastSfx",
                BindingFlags.Instance | BindingFlags.Public));
        }
    }

    [Test]
    public void GameplayAndTestBenchUseTheSharedCuePlayerAndModelIsolation()
    {
        Assert.AreEqual(
            "event:/SFX/Sex/Slow",
            SexSimulationSfxPlayer.SlowEventPath);
        Assert.AreEqual(
            "event:/SFX/Sex/Fast",
            SexSimulationSfxPlayer.FastEventPath);
        Assert.AreEqual(
            "event:/SFX/Sex/CumInside",
            SexSimulationSfxPlayer.CumInsideEventPath);
        Assert.AreEqual(
            "event:/SFX/Sex/Squirt",
            SexSimulationSfxPlayer.SquirtEventPath);

        foreach (string receiverPath in new[]
                 {
                     MissionaryReceiverPath,
                     CowgirlReceiverPath
                 })
        {
            string receiverSource = File.ReadAllText(receiverPath);
            StringAssert.Contains("SexSimulationSfxPlayer.PlayCue", receiverSource);
            StringAssert.DoesNotContain("AudioManager.Instance", receiverSource);
            StringAssert.DoesNotContain("FMODEvents.Instance", receiverSource);

            string fastSfxBody = GetMethodBody(
                receiverSource,
                "public void OnFastSfx()");
            StringAssert.Contains("SexSimulationSfxPlayer.PlayCue", fastSfxBody);
            StringAssert.Contains("SexSimulationSfxCue.Fast", fastSfxBody);
            StringAssert.DoesNotContain("SexSimulationManager", fastSfxBody);
            StringAssert.DoesNotContain(".OnThrust(", fastSfxBody);
        }

        string testBenchSource = File.ReadAllText(TestBenchPath);
        StringAssert.Contains("InitializeSexSimulationSfxPreview(root)", testBenchSource);
        StringAssert.Contains("IsolateSceneModels(root)", testBenchSource);
        StringAssert.Contains("MaintainModelIsolation()", testBenchSource);
        StringAssert.Contains("RestoreSceneModels()", testBenchSource);

        var contextObject = new GameObject("Sfx Preview Context Contract");
        try
        {
            SexSimulationSfxPreviewContext preview =
                contextObject.AddComponent<SexSimulationSfxPreviewContext>();
            preview.Initialize(SexPositionConfigType.Cowgirl);
            Assert.AreEqual(SexPositionConfigType.Cowgirl, preview.PositionType);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(contextObject);
        }
    }

    private static AnimationClip[] GetUniqueClips(string controllerPath)
    {
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        Assert.IsNotNull(controller, controllerPath);
        return controller.animationClips.Distinct().ToArray();
    }

    private static float GetFastCueValue(AnimationClip referenceClip, bool missionary)
    {
        AnimationEvent cue = AnimationUtility.GetAnimationEvents(referenceClip)
            .Single(animationEvent => animationEvent.functionName == "OnThrust");
        return GetPenetrationCurve(referenceClip, missionary).Evaluate(cue.time);
    }

    private static float[] GetMissionaryFastCueTimes(
        AnimationClip clip,
        float fastCueValue)
    {
        return GetPenetrationCurve(clip, true).keys
            .Where(keyframe =>
                keyframe.time > 0f &&
                keyframe.time <= 7.0001f &&
                Mathf.Abs(keyframe.value - fastCueValue) <= 0.0001f)
            .Select(keyframe => keyframe.time)
            .ToArray();
    }

    private static float[] GetCowgirlFastCueTimes(
        AnimationClip clip,
        float fastCueValue)
    {
        AnimationCurve curve = GetPenetrationCurve(clip, false);
        Keyframe[] keyframes = curve.keys;
        var times = new List<float>();

        for (int index = 0; index < keyframes.Length - 1; index++)
        {
            Keyframe start = keyframes[index];
            Keyframe end = keyframes[index + 1];
            if (start.time >= 7f ||
                start.value <= fastCueValue ||
                end.value > fastCueValue)
            {
                continue;
            }

            float low = start.time;
            float high = end.time;
            for (int iteration = 0; iteration < 40; iteration++)
            {
                float middle = (low + high) * 0.5f;
                if (curve.Evaluate(middle) > fastCueValue)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            times.Add((low + high) * 0.5f);
        }

        return times.ToArray();
    }

    private static AnimationCurve GetPenetrationCurve(
        AnimationClip clip,
        bool missionary)
    {
        string parameterName = missionary
            ? clip.name.Contains("Butthole")
                ? "ParamDickMoveButthole"
                : "ParamDickMovePussy"
            : "DickMove_Pussy";
        EditorCurveBinding binding = AnimationUtility.GetCurveBindings(clip)
            .Single(curveBinding =>
                curveBinding.path.EndsWith(parameterName, StringComparison.Ordinal));
        return AnimationUtility.GetEditorCurve(clip, binding);
    }

    private static string GetMethodBody(string source, string signature)
    {
        int signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.GreaterOrEqual(signatureIndex, 0, signature);
        int openingBrace = source.IndexOf('{', signatureIndex);
        Assert.GreaterOrEqual(openingBrace, 0, signature);

        int depth = 0;
        for (int index = openingBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(openingBrace, index - openingBrace + 1);
                }
            }
        }

        Assert.Fail($"Could not find the end of {signature}.");
        return string.Empty;
    }

    private static bool IsCumTarget(string clipName, bool missionary)
    {
        return missionary
            ? clipName.Contains(" Cum") &&
              clipName.Contains("(Level") &&
              !clipName.Contains("Loop") &&
              !clipName.Contains("Outside")
            : clipName.StartsWith("Cowgirl Cum", StringComparison.Ordinal) &&
              clipName.Contains("(Level") &&
              !clipName.Contains("Loop") &&
              !clipName.Contains("Outside");
    }

    private static bool IsSquirtTarget(string clipName, bool missionary)
    {
        return missionary
            ? clipName == "Missionary Squirting"
            : clipName == "Cowgirl Squirting" ||
              clipName == "Cowgirl Squirting CD";
    }

    private static void AssertCue(
        AnimationEvent animationEvent,
        float expectedTime,
        string clipName)
    {
        Assert.AreEqual(expectedTime, animationEvent.time, 0.0001f, clipName);
        Assert.AreEqual(
            SendMessageOptions.RequireReceiver,
            animationEvent.messageOptions,
            clipName);
    }
}
