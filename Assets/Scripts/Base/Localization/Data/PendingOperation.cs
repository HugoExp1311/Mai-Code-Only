using System;
using System.Collections.Generic;
using TMPro;

namespace Base.Localization.Data
{
    /// <summary>
    /// Represents a localization operation queued until a CSV domain is available.
    /// </summary>
    public struct PendingOperation
    {
        public TMP_Text Component;
        public string Key;
        public string TableName;
        public Dictionary<string, object> Variables;
        public Action<string> Callback;
    }
}
