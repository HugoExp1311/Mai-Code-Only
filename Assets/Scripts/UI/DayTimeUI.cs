using Base;
using Events;
using EventBus;
using System;
using UnityEngine;
using Base.Localization;

public class DayTimeUI : MonoBehaviour
{
    [SerializeField] private LocalizedText _day;
    [SerializeField] private LocalizedText _date;
    [SerializeField] private LocalizedText _time;

    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private DateTime _currentDateTime;

    // OPT-49: Allocate bindings once in Awake, reuse across enable/disable cycles
    void Awake()
    {
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
    }

    void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        
        LocalizationManager.Instance.LanguageChanged += HandleLocalizationChanged;
    }

    void OnDisable()
    {
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged -= HandleLocalizationChanged;
    }

    private void HandleGameStart(GameStartEvent gameStartEventArgs)
    {
        SetDateTime(gameStartEventArgs.Time);
    }

    private void HandlePlaceChanged(PlaceChangedEvent placeChangedEventArgs)
    {
        SetDateTime(placeChangedEventArgs.Time);
    }

    private void SetDateTime(DateTime dateTime)
    {
        _currentDateTime = dateTime;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_currentDateTime == default(DateTime))
        {
            SetEmpty(_day);
            SetEmpty(_date);
            SetEmpty(_time);
            return;
        }

        var culture = GameManager.Instance != null
            ? GameManager.Instance.CultureInfo
            : System.Globalization.CultureInfo.InvariantCulture;
        int dayNumber = (_currentDateTime - new DateTime(_currentDateTime.Year, 1, 1)).Days + 1;
        string timeValue = FormatTime(_currentDateTime, culture);

        _day?.SetLocalized("Day Time Day", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["day"] = dayNumber
        });
        _date?.SetLocalized(GetDayOfWeekKey(_currentDateTime.DayOfWeek), LocalizationDomains.UI);
        _time?.SetLocalized("Day Time Time", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["time"] = timeValue
        });
    }

    private void HandleLocalizationChanged(Base.Settings.Language language, string localeCode, System.Globalization.CultureInfo culture)
    {
        if (_currentDateTime != default(DateTime))
        {
            UpdateUI();
        }
    }

    private static string FormatTime(DateTime dateTime, System.Globalization.CultureInfo culture)
    {
        var customCulture = (System.Globalization.CultureInfo)culture.Clone();
        switch (culture.Name)
        {
            case "vi-VN":
                customCulture.DateTimeFormat.AMDesignator = "SA";
                customCulture.DateTimeFormat.PMDesignator = "CH";
                break;
            case "ja-JP":
                customCulture.DateTimeFormat.AMDesignator = "午前";
                customCulture.DateTimeFormat.PMDesignator = "午後";
                break;
        }

        return dateTime.Hour == 0
            ? $"00:{dateTime.ToString("mm", customCulture)} {dateTime.ToString("tt", customCulture)}"
            : dateTime.ToString("hh:mm tt", customCulture);
    }

    private static string GetDayOfWeekKey(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "In Game DayOfWeek Sunday",
            DayOfWeek.Monday => "In Game DayOfWeek Monday",
            DayOfWeek.Tuesday => "In Game DayOfWeek Tuesday",
            DayOfWeek.Wednesday => "In Game DayOfWeek Wednesday",
            DayOfWeek.Thursday => "In Game DayOfWeek Thursday",
            DayOfWeek.Friday => "In Game DayOfWeek Friday",
            DayOfWeek.Saturday => "In Game DayOfWeek Saturday",
            _ => "In Game DayOfWeek Sunday"
        };
    }

    private static void SetEmpty(LocalizedText text)
    {
        text?.SetLocalized("UI Empty", LocalizationDomains.UI);
    }

}
