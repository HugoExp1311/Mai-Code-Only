using UnityEngine;

namespace MaiLoveStory.Live2D.Rendering
{
    /// <summary>
    /// Marks a camera that must not enqueue the Cubism URP render pass.
    /// The camera and all of its non-Cubism rendering remain enabled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CubismCameraRenderExclusion : MonoBehaviour
    {
    }
}
