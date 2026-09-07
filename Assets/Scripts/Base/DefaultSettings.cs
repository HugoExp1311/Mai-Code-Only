using System.Collections.Generic;
using Base.Character.Action;
using Base.Character.Skills;
using Base.Settings;

namespace Base
{
    public static class DefaultSettings
    {
        /*Begin Player's default stats and resource*/
        public const int InitialMoney = 500;

        public const int DefaultCharming = 0;
        public const int DefaultKnowledge = 0;
        public const int DefaultStamina = 100;
        /*End Player's default stats and resource*/

        /*Begin Boss's default stats and resource*/
        public const int DefaultLove = 0;

        // === Love Level ===
        // Love Points are a cumulative, uncapped counter. Love Level is DERIVED from the
        // cumulative points using the thresholds below (it is never stored separately).
        public const int DefaultLoveLevel = 1;
        public const int MaxLoveLevel = 5;

        // Cumulative Love Points required to REACH each Love Level.
        // Level 1 is the default (0 points). Level 5 is the highest; once reached,
        // Love Point rewards/penalties default to +0 (see GetClampedLoveDelta).
        public static readonly IReadOnlyDictionary<int, int> LoveLevelThresholds =
            new Dictionary<int, int>
            {
                { 1, 0 },
                { 2, 100 },
                { 3, 200 },
                { 4, 300 },
                { 5, 400 }
            };

        // === Libido (days without sex) ===
        // Libido is modeled as the number of days since Mai last had sex (0..MaxLibido).
        // The named state shown to the player is derived from the day thresholds below.
        // States: 0 = Not interested, 1 = Normal, 2 = Ready to have sex, 3 = Horny, 4 = Craving for cock.
        public const int DefaultLibido = 0;
        public const int MaxLibido = 7;

        // Minimum days-without-sex required to reach each Libido state index.
        // Sorted ascending; GetLibidoState picks the highest state whose threshold is met.
        public static readonly IReadOnlyList<int> LibidoStateDayThresholds =
            new int[] { 0, 1, 3, 5, 7 };

        // Libido state index at or above which sex is allowed ("Ready to have sex").
        public const int LibidoReadyStateIndex = 2;
        public const int DefaultBoobsSensitivePoints = 0;
        public const int DefaultMouthSensitivePoints = 0;
        public const int DefaultPussySensitivePoints = 0;
        public const int DefaultButtholeSensitivePoints = 0;

        public const int DefaultLewdLevel = 1;
        public const int MaxLewdLevel = 5;
        public const int DefaultSensitiveLevel = 1;
        public const int MaxSensitiveLevel = 10;
        public const int DefaultPregnancyChance = 0;
        public const int MaxPregnancyChance = 100;

        // Costs to upgrade to the target Lewd Level. Pussy/Butthole share one cost,
        // Boobs/Mouth share the other; costs are deducted from all four body parts.
        public static readonly IReadOnlyDictionary<int, (int PussyButthole, int BoobsMouth)> LewdLevelUpgradeRequirements =
            new Dictionary<int, (int PussyButthole, int BoobsMouth)>
            {
                { 2, (60, 40) },
                { 3, (250, 170) },
                { 4, (600, 300) },
                { 5, (1000, 500) }
            };

        // Costs to upgrade to the target Sensitive Level for a single body part.
        public static readonly IReadOnlyDictionary<int, int> SensitiveLevelUpgradeRequirements =
            new Dictionary<int, int>
            {
                { 2, 20 },
                { 3, 50 },
                { 4, 100 },
                { 5, 150 },
                { 6, 220 },
                { 7, 300 },
                { 8, 370 },
                { 9, 480 },
                { 10, 600 }
            };

        public static bool TryGetLewdLevelUpgradeRequirement(
            int targetLevel,
            out int pussyButtholeCost,
            out int boobsMouthCost)
        {
            if (LewdLevelUpgradeRequirements.TryGetValue(targetLevel, out var requirement))
            {
                pussyButtholeCost = requirement.PussyButthole;
                boobsMouthCost = requirement.BoobsMouth;
                return true;
            }

            pussyButtholeCost = 0;
            boobsMouthCost = 0;
            return false;
        }

        public static int GetSensitiveLevelUpgradeRequirement(int targetLevel)
        {
            return SensitiveLevelUpgradeRequirements.TryGetValue(targetLevel, out int cost) ? cost : -1;
        }

        /// <summary>
        /// Derive the current Love Level (1..MaxLoveLevel) from cumulative Love Points.
        /// </summary>
        public static int GetLoveLevel(int lovePoints)
        {
            int level = DefaultLoveLevel;
            for (int candidate = DefaultLoveLevel; candidate <= MaxLoveLevel; candidate++)
            {
                if (LoveLevelThresholds.TryGetValue(candidate, out int required) && lovePoints >= required)
                {
                    level = candidate;
                }
            }
            return level;
        }

        /// <summary>
        /// Fraction (0..1) of progress from the current Love Level toward the next one.
        /// Returns 1 when already at the max Love Level.
        /// </summary>
        public static float GetLoveLevelProgress(int lovePoints)
        {
            int level = GetLoveLevel(lovePoints);
            if (level >= MaxLoveLevel)
            {
                return 1f;
            }

            int current = LoveLevelThresholds.TryGetValue(level, out int c) ? c : 0;
            int next = LoveLevelThresholds.TryGetValue(level + 1, out int n) ? n : current;
            int span = next - current;
            if (span <= 0)
            {
                return 1f;
            }

            float progress = (float)(lovePoints - current) / span;
            return progress < 0f ? 0f : (progress > 1f ? 1f : progress);
        }

        /// <summary>
        /// Clamp a proposed Love Point delta according to game rules:
        /// at the max Love Level, rewards and penalties both default to +0.
        /// Evaluated against the CURRENT level so a level-up delta can still overshoot.
        /// </summary>
        public static int GetClampedLoveDelta(int currentLovePoints, int delta)
        {
            if (GetLoveLevel(currentLovePoints) >= MaxLoveLevel)
            {
                return 0;
            }
            return delta;
        }

        /// <summary>
        /// Derive the Libido state index (0..LibidoStateDayThresholds.Count-1)
        /// from the number of days without sex.
        /// </summary>
        public static int GetLibidoState(int daysWithoutSex)
        {
            int state = 0;
            for (int i = 0; i < LibidoStateDayThresholds.Count; i++)
            {
                if (daysWithoutSex >= LibidoStateDayThresholds[i])
                {
                    state = i;
                }
            }
            return state;
        }

        /// <summary>
        /// True when Mai is willing to have sex (Libido state at or above "Ready to have sex").
        /// </summary>
        public static bool IsLibidoReadyForSex(int daysWithoutSex)
        {
            return GetLibidoState(daysWithoutSex) >= LibidoReadyStateIndex;
        }

        public static int GetSensitivePointsPerInteraction(int sensitiveLevel)
        {
            return System.Math.Max(DefaultSensitiveLevel, System.Math.Min(MaxSensitiveLevel, sensitiveLevel));
        }

        // Sensitive points decay per night without sex
        public const int SensitivePointsDecayPerNight = 10;
        /*End Boss's default stats and resource*/

        /*Begin Game default settings*/
        public const int DefaultMusicVolume = 50;
        public const int DefaultSoundVolume = 50;
        public const int DefaultVoiceVolume = 50;
        public const Language DefaultLanguage = Language.English;
        public const ScreenSettings DefaultScreenSettings = ScreenSettings.FullScreen;

        /*End Game default settings*/

        /*Begin Area Travel & Availability settings*/
        /// <summary>
        /// Travel time in minutes to reach each area
        /// </summary>
        public static readonly IReadOnlyDictionary<Area, int> AreaTravelTimes = new Dictionary<Area, int>
        {
            { Area.Home, 30 },
            { Area.HiepMart, 15 },
            { Area.Company, 30 },
            { Area.Park, 30 }
        };

        /// <summary>
        /// Area unavailability windows: (StartHour, DurationHours) or null for always-available
        /// Company: closed 17:00–07:30 (14.5h), Park: closed 18:00–06:00 (12h)
        /// </summary>
        public static readonly IReadOnlyDictionary<Area, (double Start, double Duration)?> AreaUnavailability = new Dictionary<Area, (double Start, double Duration)?>
        {
            { Area.Home, null },
            { Area.HiepMart, null },
            { Area.Company, (17, 14.5) },
            { Area.Park, (18, 12) }
        };

        /// <summary>
        /// Get travel time in minutes to the given area
        /// </summary>
        public static int GetTravelTime(Area area)
        {
            return AreaTravelTimes.TryGetValue(area, out int time) ? time : 0;
        }

        /// <summary>
        /// Check if an area is unavailable at the given time
        /// Handles unavailability windows that cross midnight
        /// </summary>
        public static bool IsAreaUnavailableAtTime(Area area, System.DateTime timeToCheck)
        {
            if (!AreaUnavailability.TryGetValue(area, out var unavailability) || !unavailability.HasValue)
                return false;

            double startHour = unavailability.Value.Start;
            double durationHours = unavailability.Value.Duration;

            if (startHour + durationHours > 24)
            {
                // Unavailable period crosses midnight
                System.DateTime todayStart = timeToCheck.Date.AddHours(startHour);
                System.DateTime todayEnd = timeToCheck.Date.AddDays(1);
                bool isInTodayPeriod = timeToCheck >= todayStart && timeToCheck < todayEnd;

                System.DateTime tomorrowStart = timeToCheck.Date;
                System.DateTime tomorrowEnd = timeToCheck.Date.AddHours(startHour + durationHours - 24);
                bool isInTomorrowPeriod = timeToCheck >= tomorrowStart && timeToCheck < tomorrowEnd;

                return isInTodayPeriod || isInTomorrowPeriod;
            }
            else
            {
                System.DateTime datePart = timeToCheck.Date;
                System.DateTime startTime = datePart.AddHours(startHour);
                System.DateTime endTime = startTime.AddHours(durationHours);
                return timeToCheck >= startTime && timeToCheck < endTime;
            }
        }
        /*End Area Travel & Availability settings*/

        /*Begin Skill default settings*/
        public const int DefaultSkillPoint = 0;
        public const int BeginLevel = 1;
        public const int MaxSkillLevel5 = 5;
        public const int MaxSkillLevel9 = 9;
        public const int MaxSkillLevel10 = 10;
        public const int MaxSkillLevel11 = 11;

        /// <summary>
        /// Default skill values at Level 1 (in tenths of percent for precision)
        /// Hand/Tongue/F/A: 5 = 0.5%, Cum: 50 = 5%, Bullet: 1 count, LongNight: 0 = 0%, Size: 100 = 10%
        /// </summary>
        public static readonly Dictionary<SkillType, int> DefaultSkillValue = new()
        {
            { SkillType.Hand, 5 },       // 0.5% orgasm rate
            { SkillType.Tongue, 5 },     // 0.5% orgasm rate
            { SkillType.F, 5 },          // 0.5% orgasm rate
            { SkillType.A, 5 },          // 0.5% orgasm rate
            { SkillType.Cum, 50 },       // 5% cumming bar increase (lower is better)
            { SkillType.Bullet, 1 },     // 1 cum shot
            { SkillType.LongNight, 0 },  // 0% stamina bonus
            { SkillType.Size, 100 },     // 10% stamina consumption (lower is better)
        };

        /// <summary>
        /// Story/progression gate for which skills are available by default.
        /// </summary>
        public static readonly Dictionary<SkillType, bool> DefaultSkillUnlocked = new()
        {
            { SkillType.Hand, true },
            { SkillType.Tongue, true },
            { SkillType.F, true },
            { SkillType.A, false },
            { SkillType.Cum, true },
            { SkillType.Bullet, true },
            { SkillType.LongNight, true },
            { SkillType.Size, true },
        };

        /// <summary>
        /// Skill value increase per level (in tenths of percent)
        /// Positive = higher is better, Negative = lower is better
        /// </summary>
        public static readonly Dictionary<SkillType, int> DefaultSkillValueIncrease = new()
        {
            { SkillType.Hand, 5 },        // +0.5% per level
            { SkillType.Tongue, 5 },      // +0.5% per level
            { SkillType.F, 5 },           // +0.5% per level
            { SkillType.A, 5 },           // +0.5% per level
            { SkillType.Cum, -5 },        // -0.5% per level (5% → 1%)
            { SkillType.Bullet, 1 },      // +1 shot per level
            { SkillType.LongNight, 10 },  // +10% stamina limit per level
            { SkillType.Size, -10 },      // -1% stamina consumption per level (10% → 1%)
        };

        /// <summary>
        /// Sex Points required to upgrade each skill at each level
        /// Format: SkillType → Level → Required Sex Points
        /// </summary>
        public static readonly Dictionary<SkillType, Dictionary<int, int>> SkillUpgradeRequirements = new()
        {
            // Magic Hand (5 levels): 100, 150, 200, 250, MAX
            { SkillType.Hand, new Dictionary<int, int>
                { {1, 100}, {2, 150}, {3, 200}, {4, 250}, {5, -1} }
            },
            // Hmmm … delicious! (5 levels): 100, 150, 200, 250, MAX
            { SkillType.Tongue, new Dictionary<int, int>
                { {1, 100}, {2, 150}, {3, 200}, {4, 250}, {5, -1} }
            },
            // You like my dick, huh? (10 levels): 200, 300, 400, 500, 600, 700, 800, 900, 1000, MAX
            { SkillType.F, new Dictionary<int, int>
                { {1, 200}, {2, 300}, {3, 400}, {4, 500}, {5, 600}, {6, 700}, {7, 800}, {8, 900}, {9, 1000}, {10, -1} }
            },
            // You like anal, don't you? (10 levels): 200, 300, 400, 500, 600, 700, 800, 900, 1000, MAX
            { SkillType.A, new Dictionary<int, int>
                { {1, 200}, {2, 300}, {3, 400}, {4, 500}, {5, 600}, {6, 700}, {7, 800}, {8, 900}, {9, 1000}, {10, -1} }
            },
            // I'm bout to CUM (9 levels): 100, 150, 200, 250, 300, 350, 400, 500, MAX
            { SkillType.Cum, new Dictionary<int, int>
                { {1, 100}, {2, 150}, {3, 200}, {4, 250}, {5, 300}, {6, 350}, {7, 400}, {8, 500}, {9, -1} }
            },
            // I need more bullet! (10 levels): 100, 150, 200, 250, 300, 350, 400, 450, 500, MAX
            { SkillType.Bullet, new Dictionary<int, int>
                { {1, 100}, {2, 150}, {3, 200}, {4, 250}, {5, 300}, {6, 350}, {7, 400}, {8, 450}, {9, 500}, {10, -1} }
            },
            // Long Night (11 levels): 100, 150, 200, 250, 300, 350, 400, 450, 500, 600, MAX
            { SkillType.LongNight, new Dictionary<int, int>
                { {1, 100}, {2, 150}, {3, 200}, {4, 250}, {5, 300}, {6, 350}, {7, 400}, {8, 450}, {9, 500}, {10, 600}, {11, -1} }
            },
            // Big Dick (10 levels): 100, 150, 200, 250, 300, 350, 400, 450, 500, 600, MAX
            { SkillType.Size, new Dictionary<int, int>
                { {1, 100}, {2, 150}, {3, 200}, {4, 250}, {5, 300}, {6, 350}, {7, 400}, {8, 450}, {9, 500}, {10, -1} }
            },
        };

        /// <summary>
        /// Max level for each skill type
        /// </summary>
        public static readonly Dictionary<SkillType, int> SkillMaxLevels = new()
        {
            { SkillType.Hand, 5 },
            { SkillType.Tongue, 5 },
            { SkillType.F, 10 },
            { SkillType.A, 10 },
            { SkillType.Cum, 9 },
            { SkillType.Bullet, 10 },
            { SkillType.LongNight, 11 },
            { SkillType.Size, 10 },
        };

        /// <summary>
        /// Sex Points received when using each skill during simulation
        /// </summary>
        public static readonly Dictionary<SkillType, int> DefaultPointReceived = new()
        {
            { SkillType.Hand, 5 },
            { SkillType.Tongue, 5 },
            { SkillType.F, 10 },
            { SkillType.A, 10 },
            { SkillType.Cum, 5 },
            { SkillType.Bullet, 5 },
            { SkillType.LongNight, 0 },  // Passive skill, no points from use
            { SkillType.Size, 5 },
        };

        /*End Skill default settings*/

        /*Begin Daily Action default settings*/
        /// <summary>
        /// Time windows for when actions are available.
        /// Format: (startHour, durationHours) or null for "always available"
        /// Example: (19, 2) means available from 19:00 to 21:00
        /// </summary>
        public static readonly Dictionary<PlayerDailyAction, (double Start, double Duration)?> DailyActionTimeWindows = new()
        {
            { PlayerDailyAction.None, null },        // Always available
            { PlayerDailyAction.Sleep, null },       // Always available
            { PlayerDailyAction.Eating, (19, 2) },   // 19:00-21:00 (7 PM to 9 PM)
            { PlayerDailyAction.Sex, (21, 2) },      // 21:00-23:00 (9 PM to 11 PM)
            { PlayerDailyAction.Working, null },     // No time restriction
            { PlayerDailyAction.Talking, null },     // No time restriction
            { PlayerDailyAction.Exercise, null },    // No time restriction
            { PlayerDailyAction.Progress, null }     // No time restriction (Company only)
        };

        /// <summary>
        /// Allowed areas for each action. If not specified, action is available everywhere.
        /// </summary>
        public static readonly Dictionary<PlayerDailyAction, Area[]> DailyActionAllowedAreas = new()
        {
            { PlayerDailyAction.None, new[] { Area.Home, Area.HiepMart, Area.Company, Area.Park } },
            { PlayerDailyAction.Sleep, new[] { Area.Home } },
            { PlayerDailyAction.Eating, new[] { Area.Home } },
            { PlayerDailyAction.Sex, new[] { Area.Home } },
            { PlayerDailyAction.Working, new[] { Area.Company } },
            { PlayerDailyAction.Talking, new[] { Area.Company, Area.Park } },
            { PlayerDailyAction.Exercise, new[] { Area.Park } },
            { PlayerDailyAction.Progress, new[] { Area.Company } }
        };
        /// <summary>
        /// Action variants enum for different intensities/choices within each action
        /// </summary>
        public enum DailyActionVariant
        {
            // Sleep variants
            SleepNap,
            SleepDeep,

            // Eating variants
            EatingWithMai,

            // Sex variants
            SexAccept,
            SexDecline,

            // Working variants
            WorkNormal,
            WorkHard,

            // Talking variants
            Talking,

            // Exercise variants
            ExerciseNormal,
            ExerciseHard
        }

        /// <summary>
        /// Reward structure for daily action variants
        /// Contains all stat/resource changes and time progression for each action
        /// </summary>
        public struct DailyActionReward
        {
            public int Stamina;
            public int MaxStamina;
            public int Knowledge;
            public int Charming;
            public int Love;
            public int Money;
            public int TimeHours;
            public int? NextDayHour; // null if not jumping to next day
            public int NextDayMinute; // used only when NextDayHour has a value
        }

        /// <summary>
        /// Complete reward configuration for each action variant
        /// Format: (Stamina, MaxStamina, Knowledge, Charming, Love, Money, TimeHours, NextDayHour, NextDayMinute)
        /// </summary>
        public static readonly Dictionary<DailyActionVariant, DailyActionReward> DailyActionRewards = new()
        {
            // Sleep variants
            { DailyActionVariant.SleepNap, new DailyActionReward
                { Stamina = 20, MaxStamina = 0, Knowledge = 0, Charming = 0, Love = 0, Money = 0, TimeHours = 2, NextDayHour = null } },
            { DailyActionVariant.SleepDeep, new DailyActionReward
                { Stamina = 80, MaxStamina = 0, Knowledge = -2, Charming = 0, Love = 0, Money = 0, TimeHours = 0, NextDayHour = 6, NextDayMinute = 30 } },
            
            // Eating variants
            { DailyActionVariant.EatingWithMai, new DailyActionReward
                { Stamina = 40, MaxStamina = 0, Knowledge = 0, Charming = 0, Love = 5, Money = 0, TimeHours = 1, NextDayHour = null } },
            
            // Sex variants
            { DailyActionVariant.SexAccept, new DailyActionReward
                { Stamina = 25, MaxStamina = 0, Knowledge = 0, Charming = 0, Love = 0, Money = 0, TimeHours = 0, NextDayHour = 7, NextDayMinute = 0 } },
            { DailyActionVariant.SexDecline, new DailyActionReward
                { Stamina = 50, MaxStamina = 0, Knowledge = 0, Charming = 0, Love = 0, Money = 0, TimeHours = 0, NextDayHour = 7, NextDayMinute = 0 } },
            
            // Working variants
            { DailyActionVariant.WorkNormal, new DailyActionReward
                { Stamina = -30, MaxStamina = 0, Knowledge = 5, Charming = 0, Love = 0, Money = 400, TimeHours = 8, NextDayHour = null } },
            { DailyActionVariant.WorkHard, new DailyActionReward
                { Stamina = -50, MaxStamina = 0, Knowledge = 10, Charming = 0, Love = 0, Money = 800, TimeHours = 8, NextDayHour = null } },
            
            // Talking variants
            { DailyActionVariant.Talking, new DailyActionReward
                { Stamina = -10, MaxStamina = 0, Knowledge = 0, Charming = 5, Love = 0, Money = 0, TimeHours = 1, NextDayHour = null } },
            
            // Exercise variants
            { DailyActionVariant.ExerciseNormal, new DailyActionReward
                { Stamina = -20, MaxStamina = 5, Knowledge = 0, Charming = 0, Love = 0, Money = 0, TimeHours = 2, NextDayHour = null } },
            { DailyActionVariant.ExerciseHard, new DailyActionReward
                { Stamina = -40, MaxStamina = 10, Knowledge = 0, Charming = 0, Love = 0, Money = 0, TimeHours = 2, NextDayHour = null } }
        };

        /*End Daily Action default settings*/

        /*Begin Work Level (Progress) Settings*/
        /// <summary>
        /// Maximum work level (1-5)
        /// </summary>
        public const int MaxWorkLevel = 5;

        /// <summary>
        /// Base rewards for working (before level multiplier)
        /// Formula: Money = 400 * multiplier, Knowledge = 5 * multiplier, Progress = 10
        /// </summary>
        public const int WorkBaseMoneyReward = 400;
        public const int WorkBaseKnowledgeReward = 5;
        public const int WorkBaseProgressReward = 10;
        public const int WorkBaseEnergyConsumption = -30;
        public const int WorkBaseTimeHours = 8;

        /// <summary>
        /// Work level multipliers (Lv1 = x1.0, Lv2 = x1.5, Lv3 = x2.0, Lv4 = x2.5, Lv5 = x3.0)
        /// </summary>
        public static readonly Dictionary<int, float> WorkLevelMultipliers = new()
        {
            { 1, 1.0f },
            { 2, 1.5f },
            { 3, 2.0f },
            { 4, 2.5f },
            { 5, 3.0f }
        };

        /// <summary>
        /// Progress points required to promote to next level
        /// Key = current level, Value = progress required to promote to next level
        /// </summary>
        public static readonly Dictionary<int, int> WorkLevelProgressRequirements = new()
        {
            { 1, 150 },   // Need 150 progress to go from Lv1 to Lv2
            { 2, 400 },   // Need 400 progress to go from Lv2 to Lv3
            { 3, 750 },   // Need 750 progress to go from Lv3 to Lv4
            { 4, 1000 },  // Need 1000 progress to go from Lv4 to Lv5
            { 5, -1 }     // Max level, no promotion possible
        };

        /// <summary>
        /// Calculate work rewards based on current work level
        /// </summary>
        public static (int money, int knowledge, int progress, int stamina, int timeHours) GetWorkRewards(int workLevel)
        {
            float multiplier = WorkLevelMultipliers.TryGetValue(workLevel, out float m) ? m : 1.0f;

            int money = (int)System.Math.Floor(WorkBaseMoneyReward * multiplier);
            int knowledge = (int)System.Math.Floor(WorkBaseKnowledgeReward * multiplier);
            int progress = (int)System.Math.Floor(WorkBaseProgressReward * multiplier); // Progress scales with level: 10 × Lv{x}
            int stamina = WorkBaseEnergyConsumption; // Energy consumption stays the same
            int timeHours = WorkBaseTimeHours; // Time stays the same

            return (money, knowledge, progress, stamina, timeHours);
        }

        /// <summary>
        /// Check if player can promote to next work level
        /// </summary>
        public static bool CanPromoteWorkLevel(int currentLevel, int currentProgress)
        {
            if (currentLevel >= MaxWorkLevel) return false;
            if (!WorkLevelProgressRequirements.TryGetValue(currentLevel, out int required)) return false;
            return currentProgress >= required;
        }
        /*End Work Level (Progress) Settings*/

        /*Begin Sex Simulation Result Settings*/

        // === Sex Points per Orgasm (by position/scene) ===

        // Roleplay scenes
        public const int RoleplayPussySexPoints = 30;     // "Roleplay Pussy" → +30 SP per orgasm
        public const int RoleplayButtholeSexPoints = 25;  // "Roleplay Butthole" → +25 SP per orgasm
        public const int RoleplayBlowjobSexPoints = 25;   // "Roleplay Blowjob" → +25 SP per orgasm
        public const int RoleplayPaizuriSexPoints = 25;   // "Roleplay Paizuri" → +25 SP per Mai orgasm

        // Sex positions
        public const int MissionarySexPoints = 35;   // +35 SP per orgasm
        public const int DoggySexPoints = 40;        // +40 SP per orgasm
        public const int CowgirlSexPoints = 45;      // +45 SP per orgasm
        public const int FullNelsonSexPoints = 50;   // +50 SP per orgasm
        public const int SpooningSexPoints = 55;     // +55 SP per orgasm
        public const int MatingPressSexPoints = 60;  // +60 SP per orgasm

        // Awarded for each Slow/Fast penetrative thrust during the simulation.
        public static readonly Dictionary<int, int> PenetrativeSexBonusPointsByLewdLevel = new()
        {
            { 1, 1 },
            { 2, 1 },
            { 3, 2 },
            { 4, 2 },
            { 5, 2 }
        };

        // === Sensitive Points per Operation (by body part) ===
        // Each operation grants points equal to that body part's current Sensitive Level.

        // === Mai's Satisfaction System ===

        /// <summary>
        /// Number of orgasms required per Lewd Level for Mai's Satisfaction
        /// </summary>
        public static readonly Dictionary<int, int> SatisfactionOrgasmRequirements = new()
        {
            { 1, 2 },   // Lewd Level 1: 2+ orgasms required
            { 2, 4 },   // Lewd Level 2: 4+ orgasms required
            { 3, 6 },   // Lewd Level 3: 6+ orgasms required
            { 4, 8 },   // Lewd Level 4: 8+ orgasms required
            { 5, 10 }   // Lewd Level 5: 10+ orgasms required
        };

        /// <summary>
        /// Love points GAINED when Mai's Satisfaction is met (per Lewd Level)
        /// </summary>
        public static readonly Dictionary<int, int> SatisfactionLoveRewards = new()
        {
            { 1, 10 },  // +10 Love Points
            { 2, 20 },  // +20 Love Points
            { 3, 30 },  // +30 Love Points
            { 4, 40 },  // +40 Love Points
            { 5, 50 }   // +50 Love Points
        };

        /// <summary>
        /// Love points LOST when Mai's Satisfaction is NOT met (per Lewd Level)
        /// Values are negative
        /// </summary>
        public static readonly Dictionary<int, int> SatisfactionLovePenalties = new()
        {
            { 1, -5 },   // -5 Love Points
            { 2, -12 },  // -12 Love Points
            { 3, -18 },  // -18 Love Points
            { 4, -24 },  // -24 Love Points
            { 5, -30 }   // -30 Love Points
        };

        // === Pregnancy ===
        public const int PregnancyChancePerCumInside = 20; // +20% per cum inside (no condom)

        // === Next Day ===
        public const int NextDayEnergyRestore = 20; // +20 Energy when starting next day

        /*End Sex Simulation Result Settings*/

        /*Begin Shop Items default settings*/
        /// <summary>
        /// Item information structure
        /// Names and descriptions are localized via the UI CSV domain using keys:
        /// - Name: "Item_{ItemId}_Name" (e.g., "Item_milk_Name")
        /// - Description: "Item_{ItemId}_Desc" (e.g., "Item_milk_Desc")
        /// </summary>
        public struct ShopItemInfo
        {
            public string ItemId;
            public ItemCategory Category;
            public Inventory.Item.ItemType Type;
            public int EffectValue;
            public int Price;
        }

        /// <summary>
        /// Item categories for shop organization
        /// </summary>
        public enum ItemCategory
        {
            Goods,
            Gift
        }

        /// <summary>
        /// All shop items indexed by item ID
        /// </summary>
        public static readonly Dictionary<string, ShopItemInfo> ShopItems = new()
        {
            { "milk", new ShopItemInfo { ItemId = "milk", Category = ItemCategory.Goods, Type = Inventory.Item.ItemType.Energy, EffectValue = 5, Price = 100 } },
            { "energy_drink", new ShopItemInfo { ItemId = "energy_drink", Category = ItemCategory.Goods, Type = Inventory.Item.ItemType.Energy, EffectValue = 10, Price = 200 } },
            { "banh_mi", new ShopItemInfo { ItemId = "banh_mi", Category = ItemCategory.Goods, Type = Inventory.Item.ItemType.Energy, EffectValue = 20, Price = 350 } },
            { "lunch", new ShopItemInfo { ItemId = "lunch", Category = ItemCategory.Goods, Type = Inventory.Item.ItemType.Energy, EffectValue = 50, Price = 850 } },
            { "condom", new ShopItemInfo { ItemId = "condom", Category = ItemCategory.Goods, Type = Inventory.Item.ItemType.Utility, EffectValue = 0, Price = 50 } },
            { "rocket_drink", new ShopItemInfo { ItemId = "rocket_drink", Category = ItemCategory.Goods, Type = Inventory.Item.ItemType.SpecialEnergy, EffectValue = 50, Price = 250 } },

            { "lipstick", new ShopItemInfo { ItemId = "lipstick", Category = ItemCategory.Gift, Type = Inventory.Item.ItemType.Love, EffectValue = 100, Price = 1500 } },
            { "flower", new ShopItemInfo { ItemId = "flower", Category = ItemCategory.Gift, Type = Inventory.Item.ItemType.Love, EffectValue = 200, Price = 2800 } },
            { "big_teddy_bear", new ShopItemInfo { ItemId = "big_teddy_bear", Category = ItemCategory.Gift, Type = Inventory.Item.ItemType.Love, EffectValue = 500, Price = 6000 } },
            { "apron", new ShopItemInfo { ItemId = "apron", Category = ItemCategory.Gift, Type = Inventory.Item.ItemType.Love, EffectValue = 100, Price = 500 } },
            { "bikini", new ShopItemInfo { ItemId = "bikini", Category = ItemCategory.Gift, Type = Inventory.Item.ItemType.Love, EffectValue = 100, Price = 1000 } },
            { "gym_outfit", new ShopItemInfo { ItemId = "gym_outfit", Category = ItemCategory.Gift, Type = Inventory.Item.ItemType.Love, EffectValue = 100, Price = 1500 } },
            { "sexy_sleepwear", new ShopItemInfo { ItemId = "sexy_sleepwear", Category = ItemCategory.Gift, Type = Inventory.Item.ItemType.Love, EffectValue = 100, Price = 2000 } }
        };
        /*End Shop Items default settings*/
    }
}
