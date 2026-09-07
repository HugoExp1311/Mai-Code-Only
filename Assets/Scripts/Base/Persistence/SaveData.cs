using System;
using System.Collections.Generic;
using System.Linq;

namespace Base.Persistence
{
    /// <summary>
    /// Serializable game save data (TaskTodo P1.1).
    /// Covers: day/time/area, player stats + money + skills, Mai stats,
    /// work level/progress, satisfaction counters, and unlock flags.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int schemaVersion = 1;

        // World state
        public string time;          // ISO-8601 DateTime (game clock, base 2001-01-01)
        public int area;             // Area enum int
        public int cycle;            // DayCycle enum int

        // Player
        public string playerName;
        public int stamina;
        public int maxStamina;
        public int charming;
        public int knowledge;
        public int money;
        public int skillPoint;
        public List<InventoryItemSaveData> inventory = new List<InventoryItemSaveData>();
        public List<SkillSaveData> skills = new List<SkillSaveData>();

        // Mai
        public int love;
        public int libido;
        public int lewdLevel;
        public int pregnancyChance;
        public List<SensitivePartSaveData> sensitiveParts = new List<SensitivePartSaveData>();

        // Work
        public int workLevel = 1;
        public int workProgress;

        // Long-term progression (P1.2 hook)
        public int satisfactionPassCount;
        public int satisfactionFailCount;

        // Unlock flags (sex scenes; events/endings to be added in P1.2)
        public List<string> unlockedSexScenes = new List<string>();

        // Daily-action tracking
        public string lastEatingDate;
        public string lastSexDate;
        public string lastWorkingDate;
        public string lastExerciseDate;
        public string lastSleepDate;
        public int napCountToday;
        public int deepSleepCountToday;

        [Serializable]
        public class SkillSaveData
        {
            public int skillType;
            public int level;
            public bool unlocked;
        }

        [Serializable]
        public class SensitivePartSaveData
        {
            public int bodyPart; // SensitiveBodyPart int
            public int points;
            public int level;
        }

        [Serializable]
        public class InventoryItemSaveData
        {
            public string id;
            public int amount;
            public int price;
            public int type; // ItemType int
            public int value;
        }
    }
}