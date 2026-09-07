using Base.Settings;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Base
{
    public class ButtonSound : MonoBehaviour
    {
        // OPT-32: Static cached yield instruction
        private static readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();
        [Header("Is Normal or Back")]
        [SerializeField] private bool isBack;

        [Header("Is Specific SFX")]
        [SerializeField] private bool isSpecificSFX;
        [SerializeField] private bool isAction;
        [SerializeField] private Area specificArea;

        private Button button;
        private UnityEngine.Events.UnityAction _currentListener;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void OnEnable()
        {
            StartCoroutine(LateStart());
        }

        void OnDisable()
        {
            // Remove the listener we added to prevent duplicates on re-enable
            if (button != null && _currentListener != null)
            {
                button.onClick.RemoveListener(_currentListener);
                _currentListener = null;
            }
        }

        private IEnumerator LateStart()
        {
            yield return _waitForFixedUpdate;

            // Remove previous listener if any (safety guard for rapid enable/disable)
            if (_currentListener != null && button != null)
            {
                button.onClick.RemoveListener(_currentListener);
            }

            if (!isSpecificSFX)
            {
                _currentListener = () =>
                {
                    AudioManager.Instance.PlayOneShot(isBack ? FMODEvents.Instance.OnBack : FMODEvents.Instance.OnButton);
                };
            }
            else
            {
                if (isAction)
                {
                    _currentListener = () => AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnAction);
                }
                else
                {
                    if ((int)specificArea >= 0 && (int)specificArea < FMODEvents.Instance.OnPlaces.Length)
                    {
                        _currentListener = () => 
                        {
                            // Check if the place is available before playing SFX
                            bool isAvailable = IsPlaceAvailable(specificArea);
#if UNITY_EDITOR
                            UnityEngine.Debug.Log($"ButtonSound: {specificArea} availability check result: {isAvailable}");
#endif
                            
                            if (isAvailable)
                            {
#if UNITY_EDITOR
                                UnityEngine.Debug.Log($"ButtonSound: Playing place SFX for {specificArea}");
#endif
                                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnPlaces[(int)specificArea]);
                            }
#if UNITY_EDITOR
                            else
                            {
                                // Don't play place SFX - the unavailable event will handle the SFX
                                UnityEngine.Debug.Log($"ButtonSound: {specificArea} is unavailable, not playing place SFX");
                            }
#endif
                        };
                    }
                }
            }

            if (_currentListener != null)
            {
                button.onClick.AddListener(_currentListener);
            }
        }

        private bool IsPlaceAvailable(Area targetArea)
        {
            // Only check availability for Company and Park (the restricted areas)
            if (targetArea != Area.Company && targetArea != Area.Park)
                return true;
                
            if (GameManager.Instance == null) 
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log("ButtonSound: GameManager.Instance is null");
#endif
                return false;
            }
            
            int travelTime = GetTravelTime(targetArea);
            System.TimeSpan travelDuration = System.TimeSpan.FromMinutes(travelTime);
            System.DateTime estimatedArrivalTime = GameManager.Instance.Time + travelDuration;
            
            bool isUnavailable = IsAreaUnavailableAtTime(targetArea, estimatedArrivalTime);
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"ButtonSound: {targetArea} at {estimatedArrivalTime:HH:mm} - isUnavailable: {isUnavailable}");
#endif
            
            return !isUnavailable;
        }

        private int GetTravelTime(Area area)
        {
            return DefaultSettings.GetTravelTime(area);
        }

        private bool IsAreaUnavailableAtTime(Area area, System.DateTime timeToCheck)
        {
            return DefaultSettings.IsAreaUnavailableAtTime(area, timeToCheck);
        }
    }
}
