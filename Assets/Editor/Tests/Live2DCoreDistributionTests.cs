using System;
using System.IO;
using System.Linq;
using Live2D.Cubism.Core;
using Live2D.Cubism.Core.Unmanaged;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class Live2DCoreDistributionTests
{
    private const string PluginRoot = "Assets/Live2D/Cubism/Plugins";
    private const string RedistributableManifestPath = PluginRoot + "/RedistributableFiles.txt";
    private const string SamplePrefabPath = "Assets/Live2D/Cubism/Samples/Models/Rice/Rice.prefab";
    private const uint ExpectedCoreVersion = 0x06000001u;

    [Test]
    public void RedistributableManifestFilesArePresent()
    {
        string[] redistributablePaths = GetRedistributablePaths();
        string[] missingPaths = redistributablePaths
            .Where(path => !File.Exists(ToAbsoluteProjectPath(path)))
            .ToArray();

        Assert.That(redistributablePaths.Length, Is.EqualTo(20),
            $"Unexpected Cubism Core redistributable count in {RedistributableManifestPath}.");
        Assert.That(missingPaths, Is.Empty,
            "Missing Cubism Core files:\n" + string.Join("\n", missingPaths));
    }

    [Test]
    public void NativeCoreMatchesCubismSdkRelease()
    {
        uint actualVersion;

        try
        {
            actualVersion = CubismCoreDll.GetVersion();
        }
        catch (Exception exception) when (
            exception is DllNotFoundException ||
            exception is EntryPointNotFoundException ||
            exception is BadImageFormatException ||
            exception.InnerException is DllNotFoundException ||
            exception.InnerException is EntryPointNotFoundException ||
            exception.InnerException is BadImageFormatException)
        {
            Assert.Fail(
                "Cubism Core could not be loaded. Ensure every file in RedistributableFiles.txt " +
                $"is present and tracked by Git.\n{exception}");
            return;
        }

        Assert.That(actualVersion, Is.EqualTo(ExpectedCoreVersion),
            $"Cubism SDK 5-r.5 requires Core {FormatCoreVersion(ExpectedCoreVersion)}, " +
            $"but the Editor loaded {FormatCoreVersion(actualVersion)}.");
    }

    [Test]
    public void BundledSampleCreatesParametersPartsAndDrawables()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabPath);
        Assert.That(prefab, Is.Not.Null, $"Cubism sample prefab was not found at {SamplePrefabPath}.");

        GameObject instance = null;

        try
        {
            instance = Object.Instantiate(prefab);
            CubismModel model = instance.GetComponentInChildren<CubismModel>(true);

            Assert.That(model, Is.Not.Null, "Cubism sample prefab does not contain a CubismModel.");
            Assert.That(model.Moc, Is.Not.Null, "Cubism sample model has no Moc reference.");
            Assert.That(model.Parameters, Is.Not.Null);
            Assert.That(model.Parameters.Length, Is.GreaterThan(0), "Cubism sample has no parameters.");
            Assert.That(model.Parts, Is.Not.Null);
            Assert.That(model.Parts.Length, Is.GreaterThan(0), "Cubism sample has no parts.");
            Assert.That(model.Drawables, Is.Not.Null);
            Assert.That(model.Drawables.Length, Is.GreaterThan(0), "Cubism sample has no drawables.");
        }
        finally
        {
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }
        }
    }

    private static string[] GetRedistributablePaths()
    {
        string manifestPath = ToAbsoluteProjectPath(RedistributableManifestPath);
        Assert.That(File.Exists(manifestPath), Is.True,
            $"Cubism redistributable manifest was not found at {RedistributableManifestPath}.");

        return File.ReadAllLines(manifestPath)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => PluginRoot + "/" + line.Substring(2))
            .ToArray();
    }

    private static string ToAbsoluteProjectPath(string projectRelativePath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string systemPath = projectRelativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, systemPath);
    }

    private static string FormatCoreVersion(uint version)
    {
        uint major = version >> 24;
        uint minor = (version >> 16) & 0xffu;
        uint patch = version & 0xffffu;
        return $"{major:D2}.{minor:D2}.{patch:D4}";
    }
}
