using System.Collections.Generic;

namespace Base
{
    /// <summary>
    /// OBSOLETE: This class contains placeholder keys for a Unity Addressables system
    /// that is not used by this project. The game uses Resources.Load and FMOD instead.
    /// Kept for reference but should not be used.
    /// </summary>
    [System.Obsolete("Addressables are not used in this project. Use Resources.Load or FMOD instead.")]
    public static class AddressableKeys
    {
        public static class Prefabs
        {
            public const string Player = "PlayerPrefab";
            public const string EnemyTypeA = "EnemyTypeA";
        }

        public static class AudioClips
        {
            public const string MainTheme = "Audio_MainTheme";
            public const string ButtonClick = "SFX_ButtonClick";
        }

        // etc.
    }
}