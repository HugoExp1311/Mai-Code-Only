using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared preferences for UI system post processors
/// </summary>
public static class UIMigrationPreferences
{
    private const string PREFS_AUTO_BUTTON = "UIMigration.AutoButton";
    private const string PREFS_AUTO_SAVE = "UIMigration.AutoSave";
    private const string PREFS_LOG_ACTIONS = "UIMigration.LogActions";
    private const string PREFS_REMOVE_BG_DETECTORS = "UIMigration.RemoveBgDetectors";
    private const string PREFS_REMOVE_OLD_REFS = "UIMigration.RemoveOldRefs";
    
    public static bool EnableAutoButtonSetup
    {
        get { return EditorPrefs.GetBool(PREFS_AUTO_BUTTON, true); }
        set { EditorPrefs.SetBool(PREFS_AUTO_BUTTON, value); }
    }
    
    public static bool AutoMigrateOnSave
    {
        get { return EditorPrefs.GetBool(PREFS_AUTO_SAVE, false); }
        set { EditorPrefs.SetBool(PREFS_AUTO_SAVE, value); }
    }
    
    public static bool LogActions
    {
        get { return EditorPrefs.GetBool(PREFS_LOG_ACTIONS, true); }
        set { EditorPrefs.SetBool(PREFS_LOG_ACTIONS, value); }
    }
    
    public static bool RemoveBackgroundClickDetectors
    {
        get { return EditorPrefs.GetBool(PREFS_REMOVE_BG_DETECTORS, true); }
        set { EditorPrefs.SetBool(PREFS_REMOVE_BG_DETECTORS, value); }
    }
    
    public static bool RemoveOldComponentReferences
    {
        get { return EditorPrefs.GetBool(PREFS_REMOVE_OLD_REFS, true); }
        set { EditorPrefs.SetBool(PREFS_REMOVE_OLD_REFS, value); }
    }
}

/// <summary>
/// Settings provider for UI Migration preferences (replaces deprecated PreferenceItem)
/// </summary>
public class UIMigrationSettingsProvider : SettingsProvider
{
    public UIMigrationSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
        : base(path, scope) { }
    
    public static bool IsAvailable()
    {
        return true;
    }
    
    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        var provider = new UIMigrationSettingsProvider("Preferences/UI Migration", SettingsScope.User);
        
        provider.keywords = new[] { "UI", "Migration", "Panel", "Button", "Auto" };
        
        return provider;
    }
    
    public override void OnGUI(string searchContext)
    {
        EditorGUILayout.LabelField("UI System Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Button Setup", EditorStyles.boldLabel);
        
        UIMigrationPreferences.EnableAutoButtonSetup = EditorGUILayout.Toggle("Enable Auto Button Setup", UIMigrationPreferences.EnableAutoButtonSetup);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Processing", EditorStyles.boldLabel);
        
        UIMigrationPreferences.AutoMigrateOnSave = EditorGUILayout.Toggle("Auto-Process on Scene Save", UIMigrationPreferences.AutoMigrateOnSave);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cleanup Settings", EditorStyles.boldLabel);
        
        UIMigrationPreferences.RemoveBackgroundClickDetectors = EditorGUILayout.Toggle("Remove Background Click Detectors", UIMigrationPreferences.RemoveBackgroundClickDetectors);
        UIMigrationPreferences.RemoveOldComponentReferences = EditorGUILayout.Toggle("Remove Old Component References", UIMigrationPreferences.RemoveOldComponentReferences);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        
        UIMigrationPreferences.LogActions = EditorGUILayout.Toggle("Log Actions", UIMigrationPreferences.LogActions);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Auto Button Setup: Automatically adds UIPanelButtonAction to Button components.\n" +
            "Remove Background Click Detectors: Cleans up old BackgroundClickDetector GameObjects.\n" +
            "Remove Old Component References: Removes references to old UI system components.\n" +
            "Auto-Process on Save: Processes scene automatically when saved.\n" +
            "Use Tools/UI Migration menu for manual processing.",
            MessageType.Info);
    }
}

