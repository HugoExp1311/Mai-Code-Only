using UI.SexPosition;
using UnityEngine;

namespace MaisLoveStory.Live2D
{
    /// <summary>
    /// Marks an isolated animation preview with the physical position it represents.
    /// Runtime receivers use this only as an alternative to live simulation state;
    /// the animation events and cue mapping remain shared with gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SexSimulationSfxPreviewContext : MonoBehaviour
    {
        [SerializeField] private SexPositionConfigType positionType;

        public SexPositionConfigType PositionType => positionType;

        public void Initialize(SexPositionConfigType value)
        {
            positionType = value;
        }
    }
}
