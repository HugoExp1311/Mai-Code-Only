using UI.SexPosition;
using UnityEngine;

namespace MaisLoveStory.Live2D
{
    /// <summary>
    /// Guarded gameplay callbacks authored onto Cowgirl motion clips.
    /// </summary>
    public sealed class CowgirlAnimationEventReceiver : MonoBehaviour
    {
        private const string FastParameter = "Fast";

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void OnThrust()
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            SexSimulationSfxPlaybackContext context = SexSimulationSfxPlayer.PlayCue(
                this,
                animator,
                SexPositionConfigType.Cowgirl,
                animator != null && animator.GetBool(FastParameter)
                    ? SexSimulationSfxCue.Fast
                    : SexSimulationSfxCue.Slow,
                manager != null && manager.IsCowgirlPlaybackActive);

            if (context == SexSimulationSfxPlaybackContext.Gameplay)
            {
                manager.OnThrust();
            }
        }

        /// <summary>
        /// Plays a Fast thrust cue without advancing gameplay accounting.
        /// Used by non-looping cum lead-ins whose visual thrusts continue after
        /// active thrust playback has stopped.
        /// </summary>
        public void OnFastSfx()
        {
            EnsureAnimator();
            SexSimulationSfxPlayer.PlayCue(
                this,
                animator,
                SexPositionConfigType.Cowgirl,
                SexSimulationSfxCue.Fast);
        }

        public void OnCumInsideSfx()
        {
            EnsureAnimator();
            SexSimulationSfxPlayer.PlayCue(
                this,
                animator,
                SexPositionConfigType.Cowgirl,
                SexSimulationSfxCue.CumInside);
        }

        public void OnSquirtSfx()
        {
            EnsureAnimator();
            SexSimulationSfxPlayer.PlayCue(
                this,
                animator,
                SexPositionConfigType.Cowgirl,
                SexSimulationSfxCue.Squirt);
        }

        public void OnWaitingLoopComplete()
        {
            if (SexSimulationSfxPlayer.IsPreviewReceiver(this))
            {
                return;
            }

            SexSimulationManager manager = SexSimulationManager.Instance;
            if (!CanDispatch(manager))
            {
                return;
            }

            manager.NotifyCowgirlAnimationSignal(
                CowgirlAnimationSignal.WaitingLoopCompleted);
        }

        private void EnsureAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private bool CanDispatch(SexSimulationManager manager)
        {
            if (manager == null || !isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            return animator != null &&
                   animator.isActiveAndEnabled &&
                   !animator.IsInTransition(0);
        }
    }
}
