using System.Collections.Generic;

namespace Base.Localization
{
    /// <summary>
    /// Compatibility registry for old editor code that used table names.
    /// Runtime localization now uses CSV domains only.
    /// </summary>
    public static class TableRegistry
    {
        public static void Initialize() { }
        public static void ClearCache() { }
        public static void RefreshTables() { }

        public static bool TableExists(string tableName)
        {
            string domain = LocalizationDomains.Normalize(tableName);
            return domain == LocalizationDomains.UI || domain == LocalizationDomains.Dialogues;
        }

        public static List<string> GetAllTableNames()
        {
            return new List<string> { LocalizationDomains.UI, LocalizationDomains.Dialogues };
        }
    }
}
