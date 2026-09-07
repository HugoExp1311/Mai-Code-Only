using UnityEngine;

namespace Live2D
{
    /// <summary>
    /// Notifies the simulation UI when either looping Roleplay after-cumming state begins.
    /// The state remains active until the player explicitly presses Stop.
    /// </summary>
    public sealed class RoleplayPussyAfterCummingStateBehaviour : StateMachineBehaviour
    {
        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            SexSimulationManager.Instance?.NotifyRoleplayAfterCummingEntered();
        }
    }
}
