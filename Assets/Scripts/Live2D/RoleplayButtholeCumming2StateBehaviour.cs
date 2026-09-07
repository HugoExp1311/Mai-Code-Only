using UnityEngine;

namespace Live2D
{
    public sealed class RoleplayButtholeCumming2StateBehaviour : StateMachineBehaviour
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
