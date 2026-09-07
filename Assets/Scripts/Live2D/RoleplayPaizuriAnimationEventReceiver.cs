using UnityEngine;

namespace Live2D
{
    /// <summary>
    /// Receives active Paizuri loop events while rejecting transition and
    /// inactive-position callbacks.
    /// </summary>
    public sealed class RoleplayPaizuriAnimationEventReceiver : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void OnPaizuriLoopComplete()
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning(
                    "[RoleplayPaizuriAnimEvents] SexSimulationManager.Instance is null!");
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
                       manager.IsRoleplayPaizuriPlaybackActive,
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
