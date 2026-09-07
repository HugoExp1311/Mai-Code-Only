using UnityEngine;
using MaisLoveStory.Live2D;
using UI.SexPosition;

namespace Live2D
{
    /// <summary>
    /// Receives animation events from Missionary animations and forwards them to SimulationNavigationPanel.
    /// This component should be attached to the Missionary Live2D GameObject.
    /// Animation events are added directly to animation clips in Unity Editor.
    /// </summary>
    public class MissionaryAnimationEventReceiver : MonoBehaviour
    {
        private const string FastParameter = "Fast";

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Called by animation event on each thrust in loop animations (Slow/Fast).
        /// Add this event at the peak of each thrust cycle in the animation.
        /// </summary>
        public void OnThrust()
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            bool gameplayPlaybackActive = manager != null &&
                                          animator != null &&
                                          IsPlaybackEventAllowed(
                                              manager.IsMissionaryPlaybackActive,
                                              animator.IsInTransition(0));
            SexSimulationSfxPlaybackContext context = SexSimulationSfxPlayer.PlayCue(
                this,
                animator,
                SexPositionConfigType.Missionary,
                animator != null && animator.GetBool(FastParameter)
                    ? SexSimulationSfxCue.Fast
                    : SexSimulationSfxCue.Slow,
                gameplayPlaybackActive);

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
                SexPositionConfigType.Missionary,
                SexSimulationSfxCue.Fast);
        }

        /// <summary>
        /// Plays the ejaculation cue at the authored visible-cum frame.
        /// </summary>
        public void OnCumInsideSfx()
        {
            EnsureAnimator();
            SexSimulationSfxPlayer.PlayCue(
                this,
                animator,
                SexPositionConfigType.Missionary,
                SexSimulationSfxCue.CumInside);
        }

        /// <summary>
        /// Plays the squirting cue at the authored visible-squirt frame.
        /// </summary>
        public void OnSquirtSfx()
        {
            EnsureAnimator();
            SexSimulationSfxPlayer.PlayCue(
                this,
                animator,
                SexPositionConfigType.Missionary,
                SexSimulationSfxCue.Squirt);
        }

        private void EnsureAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private static bool IsPlaybackEventAllowed(
            bool isMissionaryPlaybackActive,
            bool isAnimatorTransitioning)
        {
            return isMissionaryPlaybackActive && !isAnimatorTransitioning;
        }
        
        /// <summary>
        /// Called by animation event at the end of Cum Outside animations.
        /// Add this event at the last frame of (Pussy/Butthole) Cum Outside animations.
        /// </summary>
        public void OnCumOutsideComplete()
        {
            if (SexSimulationSfxPlayer.IsPreviewReceiver(this))
            {
                return;
            }

            if (SexSimulationManager.Instance != null)
            {
                SexSimulationManager.Instance.OnCumOutsideComplete();
            }
            else
            {
                Debug.LogWarning("[MissionaryAnimEvents] SexSimulationManager.Instance is null!");
            }
        }
        
        /// <summary>
        /// Called by animation event at the end of Cum Inside (CD) animations.
        /// Add this event at the last frame of (Pussy/Butthole)Cum{CD} animations.
        /// </summary>
        public void OnCumInsideComplete()
        {
            if (SexSimulationSfxPlayer.IsPreviewReceiver(this))
            {
                return;
            }

            if (SexSimulationManager.Instance != null)
            {
                SexSimulationManager.Instance.OnCumInsideComplete();
            }
            else
            {
                Debug.LogWarning("[MissionaryAnimEvents] SexSimulationManager.Instance is null!");
            }
        }
        
        /// <summary>
        /// Called by animation event at the end of Pullout animations.
        /// Add this event at the last frame of (Pussy/Butthole)PullOut animations.
        /// </summary>
        public void OnPulloutComplete()
        {
            if (SexSimulationSfxPlayer.IsPreviewReceiver(this))
            {
                return;
            }

            if (SexSimulationManager.Instance != null)
            {
                SexSimulationManager.Instance.OnPulloutComplete();
            }
            else
            {
                Debug.LogWarning("[MissionaryAnimEvents] SexSimulationManager.Instance is null!");
            }
        }
        
        /// <summary>
        /// Called by animation event at the end of Insert animations.
        /// Add this event at the last frame of (Pussy/Butthole)Insert animations.
        /// </summary>
        public void OnInsertComplete()
        {
            if (SexSimulationSfxPlayer.IsPreviewReceiver(this))
            {
                return;
            }

            if (SexSimulationManager.Instance != null)
            {
                SexSimulationManager.Instance.OnInsertComplete();
            }
            else
            {
                Debug.LogWarning("[MissionaryAnimEvents] SexSimulationManager.Instance is null!");
            }
        }
    }
}
