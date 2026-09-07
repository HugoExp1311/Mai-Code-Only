using System;
using System.Collections.Generic;
using Base.Settings;
using UnityEngine;

public class TimeManager
{
    private static readonly TimeSpan MorningEventStart = new TimeSpan(7, 0, 0);
    private static readonly TimeSpan MorningEventEnd = new TimeSpan(7, 30, 0);
    private static readonly TimeSpan EveningEventStart = new TimeSpan(18, 0, 0);
    private static readonly TimeSpan NightEventEnd = new TimeSpan(23, 0, 0);
    private static readonly TimeSpan WeekendEventStart = new TimeSpan(7, 0, 0);
    private static readonly TimeSpan WeekendEventEnd = new TimeSpan(23, 0, 0);

    public static bool IsMaiAtHome(DateTime currentTime)
    {
        DayOfWeek day = currentTime.DayOfWeek;
        TimeSpan timeOfDay = currentTime.TimeOfDay;

        bool isWeekend = (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday);

        if (isWeekend)
        {
            return timeOfDay >= WeekendEventStart && timeOfDay < WeekendEventEnd;
        }
        else
        {
            bool isMorning = timeOfDay >= MorningEventStart && timeOfDay < MorningEventEnd;
            bool isEvening = timeOfDay >= EveningEventStart && timeOfDay < NightEventEnd;
            return isMorning || isEvening;
        }
    }

    public static bool IsMusicAvailable(DayCycle cycle, DateTime currentTime)
    {
        if (MusicAvailabilityData.TryGetValue(cycle, out var availability) && availability.HasValue)
        {
            double start = availability.Value.Start;
            double duration = availability.Value.Duration;

            DateTime datePart = currentTime.Date;
            DateTime startTime = datePart.AddHours(start);
            DateTime endTime = startTime.AddHours(duration);

            return currentTime >= startTime && currentTime < endTime;
        }
        return false;
    }

    public static (DateTime Start, DateTime End)? GetMusicAvailablePeriod(DayCycle cycle, DateTime referenceTime)
    {
        if (MusicAvailabilityData.TryGetValue(cycle, out var availability) && availability.HasValue)
        {
            double start = availability.Value.Start;
            double duration = availability.Value.Duration;
            DateTime datePart = referenceTime.Date;
            DateTime startTime = datePart.AddHours(start);
            DateTime endTime = startTime.AddHours(duration);
            return (startTime, endTime);
        }
        return null;
    }

    private static readonly IReadOnlyDictionary<DayCycle, (double Start, double Duration)?> MusicAvailabilityData = new Dictionary<DayCycle, (double Start, double Duration)?>
    {
        { DayCycle.Morning, (6, 12) },
        { DayCycle.Evening, (18, 12) }
    };

    /// <summary>
    /// Check if current time is within specified hour range (24-hour format)
    /// </summary>
    /// <param name="time">Time to check</param>
    /// <param name="startHour">Start hour (0-23)</param>
    /// <param name="endHour">End hour (0-23)</param>
    /// <returns>True if time is within range</returns>
    public static bool IsTimeInRange(DateTime time, int startHour, int endHour)
    {
        int currentHour = time.Hour;
        
        if (startHour <= endHour)
        {
            // Normal range (e.g., 7 to 21)
            return currentHour >= startHour && currentHour < endHour;
        }
        else
        {
            // Range crosses midnight (e.g., 23 to 2)
            return currentHour >= startHour || currentHour < endHour;
        }
    }

    /// <summary>
    /// Check if two dates are on the same day
    /// </summary>
    /// <param name="date1">First date</param>
    /// <param name="date2">Second date</param>
    /// <returns>True if both dates are on the same day</returns>
    public static bool IsSameDay(DateTime date1, DateTime date2)
    {
        return date1.Date == date2.Date;
    }
}
