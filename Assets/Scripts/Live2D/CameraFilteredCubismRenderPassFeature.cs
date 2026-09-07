using Live2D.Cubism.Rendering.URP;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MaiLoveStory.Live2D.Rendering
{
    /// <summary>
    /// Uses the official Cubism render pass for every camera except cameras
    /// explicitly marked with <see cref="CubismCameraRenderExclusion"/>.
    /// </summary>
    public sealed class CameraFilteredCubismRenderPassFeature : CubismRenderPassFeature
    {
        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (camera != null &&
                camera.TryGetComponent(out CubismCameraRenderExclusion exclusion) &&
                exclusion.isActiveAndEnabled)
            {
                return;
            }

            base.AddRenderPasses(renderer, ref renderingData);
        }
    }
}
