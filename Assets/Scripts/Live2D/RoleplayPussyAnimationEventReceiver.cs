using UnityEngine;

namespace Live2D
{
    /// <summary>
    /// Receives authored Roleplay Pussy animation events and forwards only events
    /// produced by an active Roleplay playback to the simulation manager.
    /// </summary>
    public class RoleplayPussyAnimationEventReceiver : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void OnThrust()
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[RoleplayPussyAnimEvents] SexSimulationManager.Instance is null!");
                return;
            }

            if (CanProcessPlaybackEvent(manager))
            {
                manager.OnRoleplayThrust();
            }
        }

        public void OnToyLoopComplete()
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[RoleplayPussyAnimEvents] SexSimulationManager.Instance is null!");
                return;
            }

            if (CanProcessPlaybackEvent(manager))
            {
                manager.OnRoleplayToyLoopComplete();
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
                       manager.IsRoleplayPussyPlaybackActive,
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
