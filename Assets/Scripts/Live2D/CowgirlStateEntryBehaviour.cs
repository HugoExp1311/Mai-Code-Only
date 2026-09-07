using UI.SexPosition;
using UnityEngine;

namespace MaisLoveStory.Live2D
{
    /// <summary>
    /// Forwards authoritative Cowgirl loop-entry boundaries to gameplay.
    /// </summary>
    public sealed class CowgirlStateEntryBehaviour : StateMachineBehaviour
    {
        [SerializeField] private CowgirlAnimationSignal signal;

        public CowgirlAnimationSignal Signal
        {
            get => signal;
            set => signal = value;
        }

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            SexSimulationManager.Instance?.NotifyCowgirlAnimationSignal(signal);
        }
    }
}
