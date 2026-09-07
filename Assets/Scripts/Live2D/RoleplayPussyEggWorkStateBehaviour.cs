using UnityEngine;

namespace Live2D
{
    /// <summary>
    /// Acknowledges the delayed Egg Work loop after the insert sequence finishes.
    /// </summary>
    public sealed class RoleplayPussyEggWorkStateBehaviour : StateMachineBehaviour
    {
        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            SexSimulationManager.Instance?.NotifyRoleplayEggWorkEntered();
        }
    }
}
