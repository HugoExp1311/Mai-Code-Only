using UnityEngine;

namespace Live2D
{
    public sealed class RoleplayButtholeStartStateBehaviour : StateMachineBehaviour
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
