using UnityEngine;

namespace Live2D
{
    /// <summary>
    /// Acknowledges that the Roleplay Animator has reached its Start hub.
    /// Pending cross-family playback is dispatched only after this callback.
    /// </summary>
    public sealed class RoleplayPussyStartStateBehaviour : StateMachineBehaviour
    {
        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            SexSimulationManager.Instance?.NotifyRoleplayStartEntered();
        }
    }
}
