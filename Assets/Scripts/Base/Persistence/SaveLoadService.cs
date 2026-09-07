using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Base.Persistence
{
    /// <summary>
    /// Save/load service (TaskTodo P1.1).
    /// Quick Save slot + numbered manual save slots, stored as JSON in PlayerPrefs.
    /// </summary>
    public static class SaveLoadService
    {
        /// <summary>Number of numbered manual save slots. 5 matches the Main Menu Load Game panel (Quick Save row + 5 rows).</summary>
        public const int SlotCount = 5;
        public const string QuickSaveSlotId = "Quick";

        /// <summary>Number of rolling auto-save slots (one per day, oldest is overwritten).</summary>
        public const int AutoSlotCount = 3;
        public const string AutoSlotPrefix = "AutoSave_";

        private const string KeyPrefix = "SaveSlot_";
        private const string MetaKey = "SaveMeta_";

        public static string GetSlotKey(string slotId) => KeyPrefix + slotId;
        public static string GetMetaKey(string slotId) => MetaKey + slotId;

        /// <summary>Returns the slot id for a 1-based auto-save index, e.g. "AutoSave_1".</summary>
        public static string GetAutoSlotId(int index) => $"{AutoSlotPrefix}{index}";

        public static bool HasSave(string slotId)
        {
            return PlayerPrefs.HasKey(GetSlotKey(slotId));
        }

        public static bool HasAnySave()
        {
            if (HasSave(QuickSaveSlotId)) return true;
            for (int i = 1; i <= SlotCount; i++)
            {
                if (HasSave(i.ToString())) return true;
            }
            return false;
        }

        /// <summary>Write raw save data to a slot with metadata.</summary>
        public static void Write(string slotId, SaveData data, string label = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(GetSlotKey(slotId), json);
            PlayerPrefs.SetString(GetMetaKey(slotId), JsonUtility.ToJson(new SaveSlotMeta
            {
                slotId = slotId,
                label = label ?? slotId,
                realTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                gameDay = 0, // filled by caller via label parsing if needed; kept for future UI
                schemaVersion = data.schemaVersion
            }));
            PlayerPrefs.Save();
        }

        /// <summary>Read save data from a slot. Returns null when missing or corrupt.</summary>
        public static SaveData Read(string slotId)
        {
            if (!HasSave(slotId)) return null;
            try
            {
                string json = PlayerPrefs.GetString(GetSlotKey(slotId));
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadService] Failed to read slot '{slotId}': {e.Message}");
                return null;
            }
        }

        public static SaveSlotMeta ReadMeta(string slotId)
        {
            if (!PlayerPrefs.HasKey(GetMetaKey(slotId))) return null;
            try
            {
                return JsonUtility.FromJson<SaveSlotMeta>(PlayerPrefs.GetString(GetMetaKey(slotId)));
            }
            catch
            {
                return null;
            }
        }

        public static void Delete(string slotId)
        {
            PlayerPrefs.DeleteKey(GetSlotKey(slotId));
            PlayerPrefs.DeleteKey(GetMetaKey(slotId));
            PlayerPrefs.Save();
        }

        /// <summary>Enumerate existing manual slots: Quick, then 1..SlotCount.</summary>
        public static IEnumerable<SaveSlotMeta> ListSlots()
        {
            var ids = new List<string> { QuickSaveSlotId };
            for (int i = 1; i <= SlotCount; i++) ids.Add(i.ToString());
            return ids.Where(HasSave).Select(ReadMeta).Where(m => m != null);
        }

        /// <summary>Enumerate existing auto-save slots: AutoSave_1..AutoSave_{AutoSlotCount}, oldest first.</summary>
        public static IEnumerable<SaveSlotMeta> ListAutoSlots()
        {
            var ids = new List<string>();
            for (int i = 1; i <= AutoSlotCount; i++) ids.Add(GetAutoSlotId(i));
            return ids.Where(HasSave).Select(ReadMeta).Where(m => m != null);
        }

        /// <summary>
        /// Returns the auto-save slot id that should receive the next daily auto-save.
        /// Uses the day-of-month modulo AutoSlotCount to pick a stable slot, so saving
        /// on day D always targets the same slot (overwriting yesterday's entry in that
        /// slot) while other days keep their own slots. The result is 1-based.
        /// </summary>
        public static string GetAutoSlotIdForDay(int dayNumber)
        {
            int index = ((dayNumber - 1) % AutoSlotCount) + 1;
            return GetAutoSlotId(index);
        }

        [Serializable]
        public class SaveSlotMeta
        {
            public string slotId;
            public string label;
            public string realTimeStamp;
            public int gameDay;
            public int schemaVersion;
        }
    }
}