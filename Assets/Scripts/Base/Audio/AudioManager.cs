using Base.Settings;
using Events;
using EventBus;
using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private bool isSFXAllow = true;

        private Bus masterBus;
        private Bus voiceBus;
        private Bus musicBus;
        private Bus sfxBus;

        private List<EventInstance> eventInstances;
        
        private EventInstance ambienceEventInstance;
        private EventInstance musicEventInstance;

        private EventBinding<GameStartEvent> gameStartBinding;
        private EventBinding<PlaceChangedEvent> placeChangedBinding;
        private EventBinding<PlaceUnavailableEvent> placeUnavailableBinding;

        void Awake()
        {
            Instance = this;
            eventInstances = new();

            masterBus = RuntimeManager.GetBus("bus:/");
            musicBus = RuntimeManager.GetBus("bus:/Music");
            sfxBus = RuntimeManager.GetBus("bus:/SFX");
            
            // Voice bus may not exist yet - handle gracefully
            try
            {
                voiceBus = RuntimeManager.GetBus("bus:/Voice");
            }
            catch (BusNotFoundException)
            {
                UnityEngine.Debug.LogWarning("[AudioManager] Voice bus not found. Please rebuild FMOD banks.");
            }
            
            // OPT-49: Allocate bindings once in Awake, reuse across enable/disable cycles
            gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
            placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
            placeUnavailableBinding = new EventBinding<PlaceUnavailableEvent>(HandlePlaceUnavailable);
        }

        void OnEnable()
        {
            EventBus<GameStartEvent>.Register(gameStartBinding);
            EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
            EventBus<PlaceUnavailableEvent>.Register(placeUnavailableBinding);
        }

        void OnDisable()
        {
            // OPT-49: Deregister only — bindings are reused, not reallocated
            EventBus<GameStartEvent>.Deregister(gameStartBinding);
            EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
            EventBus<PlaceUnavailableEvent>.Deregister(placeUnavailableBinding);
        }

        void OnDestroy()
        {
            CleanUp();
            
            // Ensure bindings are deregistered on destroy
            EventBus<GameStartEvent>.Deregister(gameStartBinding);
            EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
            EventBus<PlaceUnavailableEvent>.Deregister(placeUnavailableBinding);
        }



        // OPT-4: Cache raw int volumes — int comparison is cheaper than float division + float comparison
        private int _lastMusicVolumeRaw = -1;
        private int _lastSfxVolumeRaw = -1;
        private int _lastVoiceVolumeRaw = -1;

        void Update()
        {
            int musicRaw = GameManager.Instance.SettingsManager.GetMusicVolume();
            if (musicRaw != _lastMusicVolumeRaw)
            {
                _lastMusicVolumeRaw = musicRaw;
                musicBus.setVolume(musicRaw / 100f);
            }
            
            int sfxRaw = GameManager.Instance.SettingsManager.GetSoundVolume();
            if (sfxRaw != _lastSfxVolumeRaw)
            {
                _lastSfxVolumeRaw = sfxRaw;
                sfxBus.setVolume(sfxRaw / 100f);
            }
            
            // Only set voice volume if bus was found
            if (voiceBus.isValid())
            {
                int voiceRaw = GameManager.Instance.SettingsManager.GetVoiceVolume();
                if (voiceRaw != _lastVoiceVolumeRaw)
                {
                    _lastVoiceVolumeRaw = voiceRaw;
                    voiceBus.setVolume(voiceRaw / 100f);
                }
            }
        }

        private void InitializeAmbience(EventReference ambienceEventReference)
        {
            if (eventInstances.Contains(ambienceEventInstance))
                return;

            ambienceEventInstance = CreateInstance(ambienceEventReference);
            ambienceEventInstance.start();
        }

        private void InitializeMusic(EventReference musicEventReference)
        {
            if (eventInstances.Contains(musicEventInstance))
                return;

            musicEventInstance = CreateInstance(musicEventReference);
            musicEventInstance.start();
        }

        public void SetMusic(DayCycle cycle)
        {
            musicEventInstance.setParameterByName("DayCycle", (float)cycle); 
        }

        public void PlayOneShot(EventReference sound)
        {
            // Always allow OnNotice SFX (unavailable sound) regardless of isSFXAllow state
            if (FMODEvents.Instance != null && sound.Guid == FMODEvents.Instance.OnNotice.Guid)
            {
                RuntimeManager.PlayOneShot(sound);
                return;
            }
            
            if (isSFXAllow)
            {
                // Check if this is a place SFX that should be blocked
                if (ShouldBlockPlaceSFX(sound))
                {
                    UnityEngine.Debug.Log($"AudioManager: Place SFX blocked due to unavailability - {sound}");
                    return;
                }
                
                RuntimeManager.PlayOneShot(sound);
            }
            else
            {
                UnityEngine.Debug.Log($"AudioManager: SFX blocked - {sound}");
            }
        }

        private bool ShouldBlockPlaceSFX(EventReference sound)
        {
            // Check if this is a place SFX and if the place is unavailable
            if (FMODEvents.Instance != null && FMODEvents.Instance.OnPlaces != null)
            {
                for (int i = 0; i < FMODEvents.Instance.OnPlaces.Length; i++)
                {
                    if (FMODEvents.Instance.OnPlaces[i].Guid == sound.Guid)
                    {
                        // This is a place SFX, check if the place is available
                        Area area = (Area)i;
                        return IsPlaceCurrentlyUnavailable(area);
                    }
                }
            }
            return false;
        }

        private bool IsPlaceCurrentlyUnavailable(Area area)
        {
            if (GameManager.Instance == null) return false;
            
            int travelTime = DefaultSettings.GetTravelTime(area);
            System.TimeSpan travelDuration = System.TimeSpan.FromMinutes(travelTime);
            System.DateTime estimatedArrivalTime = GameManager.Instance.Time + travelDuration;
            
            return DefaultSettings.IsAreaUnavailableAtTime(area, estimatedArrivalTime);
        }

        private int GetTravelTimeForArea(Area area)
        {
            return DefaultSettings.GetTravelTime(area);
        }

        public EventInstance CreateInstance(EventReference eventReference)
        {
            EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
            eventInstances.Add(eventInstance);
            return eventInstance;
        }

        private void HandleGameStart(GameStartEvent gameStartEventArgs)
        {
            InitializeMusic(FMODEvents.Instance.Music);
            SetMusic(gameStartEventArgs.Cycle);
        }

        private void HandlePlaceChanged(PlaceChangedEvent placeChangedEventArgs)    
        {
            SetMusic(placeChangedEventArgs.Cycle);
        }

        private void HandlePlaceUnavailable(PlaceUnavailableEvent placeUnavailableEventArgs)
        {
            // Play unavailable SFX immediately
            RuntimeManager.PlayOneShot(FMODEvents.Instance.OnNotice);
            // Disable other SFX briefly to prevent button SFX from playing
            StartCoroutine(TemporaryNotAllowSFX());
        }

        private IEnumerator TemporaryNotAllowSFX()
        {
            isSFXAllow = false;
            yield return new WaitForSeconds(0.5f); // Wait even longer to ensure button SFX doesn't play
            isSFXAllow = true;
        }

        private void CleanUp()
        {
            foreach (EventInstance eventInstance in eventInstances)
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
            }
        }
    }
}
