using System.Linq;
using Live2D.Cubism.Rendering.URP;
using MaiLoveStory.Live2D.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class Live2DRenderingConfigurationTests
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string PcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
    private const string MobilePipelinePath = "Assets/Settings/Mobile_RPAsset.asset";

    [Test]
    public void PipelinesUseSingleRendererAndPcCubismFeatureIsCameraFiltered()
    {
        UniversalRenderPipelineAsset pcPipeline = LoadPipeline(PcPipelinePath);
        UniversalRenderPipelineAsset mobilePipeline = LoadPipeline(MobilePipelinePath);

        ScriptableRendererData pcDefault = GetRendererData(pcPipeline, 0);
        ScriptableRendererData mobileDefault = GetRendererData(mobilePipeline, 0);

        Assert.That(GetRendererCount(pcPipeline), Is.EqualTo(1),
            "PC pipeline must use one renderer; per-camera Cubism filtering belongs to its feature.");
        Assert.That(GetRendererCount(mobilePipeline), Is.EqualTo(1),
            "Mobile pipeline must keep its single platform-specific renderer.");
        Assert.That(CountActiveCubismFeatures(pcDefault), Is.EqualTo(1),
            "PC renderer must keep exactly one active Cubism render feature.");
        Assert.That(pcDefault.rendererFeatures.Count(feature =>
                feature is CameraFilteredCubismRenderPassFeature && feature.isActive),
            Is.EqualTo(1),
            "The active PC Cubism feature must support explicit per-camera exclusion.");
        Assert.That(CountActiveCubismFeatures(mobileDefault), Is.Zero,
            "The existing Mobile renderer does not contain a Cubism feature.");
    }

    [Test]
    public void ParticleCameraUsesDefaultRendererWithCubismExclusion()
    {
        Scene scene = SceneManager.GetSceneByPath(GameScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;

        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        }

        try
        {
            Camera[] cameras = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .ToArray();
            Camera particleCamera = cameras.Single(camera => camera.name == "Particle Camera");
            Camera mainCamera = cameras.Single(camera => camera.name == "Main Camera");

            Assert.That(GetRendererIndex(particleCamera), Is.EqualTo(-1),
                "Particle Camera must use the pipeline's single default renderer.");
            Assert.That(GetRendererIndex(mainCamera), Is.EqualTo(-1),
                "Main Camera must continue using the default renderer at index 0.");
            CubismCameraRenderExclusion exclusion =
                particleCamera.GetComponent<CubismCameraRenderExclusion>();
            Assert.That(exclusion, Is.Not.Null,
                "Particle Camera must explicitly exclude only its Cubism render pass.");
            Assert.That(exclusion.isActiveAndEnabled, Is.True,
                "Particle Camera's Cubism exclusion marker must be active.");
            Assert.That(particleCamera.targetTexture, Is.Not.Null,
                "Particle Camera must continue rendering into its particle RenderTexture.");
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static UniversalRenderPipelineAsset LoadPipeline(string path)
    {
        UniversalRenderPipelineAsset pipeline =
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        Assert.That(pipeline, Is.Not.Null, $"URP pipeline asset was not found at {path}.");
        return pipeline;
    }

    private static ScriptableRendererData GetRendererData(
        UniversalRenderPipelineAsset pipeline,
        int index)
    {
        SerializedProperty rendererDataList =
            new SerializedObject(pipeline).FindProperty("m_RendererDataList");

        Assert.That(rendererDataList, Is.Not.Null,
            $"{pipeline.name} does not expose m_RendererDataList.");
        Assert.That(rendererDataList.arraySize, Is.GreaterThan(index),
            $"{pipeline.name} has no renderer at index {index}.");

        ScriptableRendererData rendererData = rendererDataList
            .GetArrayElementAtIndex(index)
            .objectReferenceValue as ScriptableRendererData;
        Assert.That(rendererData, Is.Not.Null,
            $"{pipeline.name} renderer index {index} is missing.");
        return rendererData;
    }

    private static int GetRendererCount(UniversalRenderPipelineAsset pipeline)
    {
        SerializedProperty rendererDataList =
            new SerializedObject(pipeline).FindProperty("m_RendererDataList");

        Assert.That(rendererDataList, Is.Not.Null,
            $"{pipeline.name} does not expose m_RendererDataList.");
        return rendererDataList.arraySize;
    }

    private static int CountActiveCubismFeatures(ScriptableRendererData rendererData)
    {
        return rendererData.rendererFeatures.Count(feature =>
            feature is CubismRenderPassFeature && feature.isActive);
    }

    private static int GetRendererIndex(Camera camera)
    {
        UniversalAdditionalCameraData cameraData =
            camera.GetComponent<UniversalAdditionalCameraData>();
        Assert.That(cameraData, Is.Not.Null,
            $"{camera.name} has no UniversalAdditionalCameraData component.");

        return new SerializedObject(cameraData)
            .FindProperty("m_RendererIndex")
            .intValue;
    }
}
