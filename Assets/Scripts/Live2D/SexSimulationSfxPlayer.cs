using System;
using Base;
using FMODUnity;
using UI.SexPosition;
using UnityEngine;

namespace MaisLoveStory.Live2D
{
    public enum SexSimulationSfxCue
    {
        Slow,
        Fast,
        CumInside,
        Squirt
    }

    public enum SexSimulationSfxPlaybackContext
    {
        None,
        Gameplay,
        Preview
    }

    /// <summary>
    /// Shared dispatch for physical-position animation SFX. Gameplay uses the
    /// simulation manager as its context; isolated tools provide a preview marker.
    /// </summary>
    public static class SexSimulationSfxPlayer
    {
        public const string SlowEventPath = "event:/SFX/Sex/Slow";
        public const string FastEventPath = "event:/SFX/Sex/Fast";
        public const string CumInsideEventPath = "event:/SFX/Sex/CumInside";
        public const string SquirtEventPath = "event:/SFX/Sex/Squirt";

        public static SexSimulationSfxPlaybackContext PlayCue(
            MonoBehaviour receiver,
            Animator animator,
            SexPositionConfigType positionType,
            SexSimulationSfxCue cue,
            bool gameplayPlaybackActive = true)
        {
            SexSimulationSfxPlaybackContext context = GetPlaybackContext(
                receiver,
                animator,
                positionType,
                gameplayPlaybackActive);
            if (context == SexSimulationSfxPlaybackContext.None)
            {
                return context;
            }

            TryPlayOneShot(cue);
            return context;
        }

        public static bool IsPreviewReceiver(Component receiver)
        {
            return receiver != null &&
                   receiver.GetComponentInParent<SexSimulationSfxPreviewContext>() != null;
        }

        private static SexSimulationSfxPlaybackContext GetPlaybackContext(
            MonoBehaviour receiver,
            Animator animator,
            SexPositionConfigType positionType,
            bool gameplayPlaybackActive)
        {
            if (receiver == null ||
                !receiver.isActiveAndEnabled ||
                !receiver.gameObject.activeInHierarchy ||
                animator == null ||
                !animator.isActiveAndEnabled ||
                animator.IsInTransition(0))
            {
                return SexSimulationSfxPlaybackContext.None;
            }

            SexSimulationSfxPreviewContext preview =
                receiver.GetComponentInParent<SexSimulationSfxPreviewContext>();
            if (preview != null &&
                preview.isActiveAndEnabled &&
                preview.gameObject.activeInHierarchy &&
                preview.PositionType == positionType)
            {
                return SexSimulationSfxPlaybackContext.Preview;
            }

            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager != null &&
                gameplayPlaybackActive &&
                manager.CurrentPositionType == positionType)
            {
                return SexSimulationSfxPlaybackContext.Gameplay;
            }

            return SexSimulationSfxPlaybackContext.None;
        }

        private static void TryPlayOneShot(SexSimulationSfxCue cue)
        {
            try
            {
                EventReference sound = ResolveEventReference(cue);
                if (sound.IsNull)
                {
                    return;
                }

                AudioManager audioManager = AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlayOneShot(sound);
                }
                else
                {
                    // The isolated Test Bench intentionally does not require game
                    // singletons. FMOD's runtime path still routes through bus:/SFX.
                    RuntimeManager.PlayOneShot(sound);
                }
            }
            catch (Exception exception)
            {
                // Audio must never prevent the animation event's gameplay callback.
                Debug.LogWarning(
                    $"[SexSimulationSfx] Could not play {cue}: {exception.Message}");
            }
        }

        private static EventReference ResolveEventReference(SexSimulationSfxCue cue)
        {
            FMODEvents events = FMODEvents.Instance;
            EventReference sound = events == null
                ? default
                : cue switch
                {
                    SexSimulationSfxCue.Slow => events.SexSlow,
                    SexSimulationSfxCue.Fast => events.SexFast,
                    SexSimulationSfxCue.CumInside => events.SexCumInside,
                    SexSimulationSfxCue.Squirt => events.SexSquirt,
                    _ => default
                };

            return sound.IsNull
                ? RuntimeManager.PathToEventReference(GetEventPath(cue))
                : sound;
        }

        private static string GetEventPath(SexSimulationSfxCue cue)
        {
            return cue switch
            {
                SexSimulationSfxCue.Slow => SlowEventPath,
                SexSimulationSfxCue.Fast => FastEventPath,
                SexSimulationSfxCue.CumInside => CumInsideEventPath,
                SexSimulationSfxCue.Squirt => SquirtEventPath,
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, null)
            };
        }
    }
}
