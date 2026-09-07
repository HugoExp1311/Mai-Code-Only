using Base;
using Base.Settings;
using Events;
using EventBus;
using UnityEngine;
using UnityEngine.UI;

public class PlaceUI : MonoBehaviour
{
    [SerializeField] private Area area;
    [SerializeField] private Image background;
    [SerializeField] private Base.Core.KeyValuePair<int, Sprite>[] timeBasedBackgrounds;

    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;

    // OPT-49: Allocate bindings once in Awake, reuse across enable/disable cycles
    void Awake()
    {
        if (background == null)
            background = GetComponent<Image>();
        
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
    }

    void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
    }

    void OnDisable()
    {
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
    }



    private void HandlePlaceChanged(PlaceChangedEvent placeChangedEventArgs)
    {
        if (timeBasedBackgrounds.Length <= 0)
            return;

        for (int i = 0; i < timeBasedBackgrounds.Length; i++)
        {
            if (placeChangedEventArgs.Time.Hour < timeBasedBackgrounds[i].Key)
            {
                background.sprite = timeBasedBackgrounds[i].Value;
                break;
            }
        }
    }

    private void HandleGameStart(GameStartEvent gameStartEventArgs)
    {
        // The data is the same, so we can just reuse the existing handler.
        HandlePlaceChanged(new PlaceChangedEvent
        {
            Area = gameStartEventArgs.Area,
            Time = gameStartEventArgs.Time,
            Cycle = gameStartEventArgs.Cycle
        });
    }

}
