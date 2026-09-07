using UnityEngine;

namespace Live2D
{
    /// <summary>
    /// Receives Roleplay Blowjob loop events and rejects callbacks emitted while
    /// the Animator is switching states or this position is inactive.
    /// </summary>
    public sealed class RoleplayBlowjobAnimationEventReceiver : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void OnBlowjobLoopComplete()
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning(
                    "[RoleplayBlowjobAnimEvents] SexSimulationManager.Instance is null!");
                return;
            }

            if (CanProcessPlaybackEvent(manager))
            {
                manager.OnRoleplayLoopComplete();
            }
        }

        private bool CanProcessPlaybackEvent(SexSimulationManager manager)
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            return animator != null &&
                   animator.isActiveAndEnabled &&
                   IsPlaybackEventAllowed(
                       manager.IsRoleplayBlowjobPlaybackActive,
                       animator.IsInTransition(0));
        }

        private static bool IsPlaybackEventAllowed(
            bool isRoleplayPlaybackActive,
            bool isAnimatorTransitioning)
        {
            return isRoleplayPlaybackActive && !isAnimatorTransitioning;
        }
    }
}
