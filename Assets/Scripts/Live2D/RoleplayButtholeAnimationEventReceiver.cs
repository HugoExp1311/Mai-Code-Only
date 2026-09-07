using UnityEngine;

namespace Live2D
{
    /// <summary>
    /// Receives authored Roleplay Butthole clip events and rejects callbacks
    /// emitted while the Animator is switching states or the position is idle.
    /// </summary>
    public sealed class RoleplayButtholeAnimationEventReceiver : MonoBehaviour
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
                Debug.LogWarning(
                    "[RoleplayButtholeAnimEvents] SexSimulationManager.Instance is null!");
                return;
            }

            if (CanProcessPlaybackEvent(manager))
            {
                manager.OnRoleplayThrust();
            }
        }

        public void OnEggLoopComplete()
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning(
                    "[RoleplayButtholeAnimEvents] SexSimulationManager.Instance is null!");
                return;
            }

            if (CanProcessPlaybackEvent(manager))
            {
                manager.OnRoleplayButtholeEggLoopComplete();
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
                       manager.IsRoleplayButtholePlaybackActive,
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
