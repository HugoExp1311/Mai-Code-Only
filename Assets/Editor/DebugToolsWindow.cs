using System;
using Base.Localization;
using Base.Settings;
using UnityEditor;
using UnityEngine;

public class DebugToolsWindow : EditorWindow
{
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Debug Tools")]
    public static void ShowWindow()
    {
        var window = GetWindow<DebugToolsWindow>("Debug Tools");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawTimeControls();
        EditorGUILayout.Space(10);
        DrawAreaControls();
        EditorGUILayout.Space(10);
        DrawIntroControls();
        EditorGUILayout.Space(10);
        DrawLocalizationControls();
        EditorGUILayout.Space(10);
        DrawCurrentStatus();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawTimeControls()
    {
        EditorGUILayout.LabelField("Time Controls", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use time controls.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        
        EditorGUILayout.LabelField("Skip to Time of Day", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Morning (7AM)")) SkipToTime(7, 0);
        if (GUILayout.Button("Evening (6PM)")) SkipToTime(18, 0);
        if (GUILayout.Button("Night (9PM)")) SkipToTime(21, 0);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Quick Skip", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+1 Hour")) SkipHours(1);
        if (GUILayout.Button("+4 Hours")) SkipHours(4);
        if (GUILayout.Button("Next Day")) SkipToNextDay();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Specific Times", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("9PM (Sex Action)")) SkipToTime(21, 0);
        if (GUILayout.Button("7AM (Morning)")) SkipToTime(7, 0);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawAreaControls()
    {
        EditorGUILayout.LabelField("Area Teleport", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use area controls.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Home")) TeleportToArea(Area.Home);
        if (GUILayout.Button("Company")) TeleportToArea(Area.Company);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Park")) TeleportToArea(Area.Park);
        if (GUILayout.Button("HiepMart")) TeleportToArea(Area.HiepMart);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawIntroControls()
    {
        EditorGUILayout.LabelField("Intro Controls", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use intro controls.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        
        if (GameManager.Instance == null)
        {
            EditorGUILayout.HelpBox("GameManager not found.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }
        
        // Current intro status
        bool isPlaying = GameManager.Instance.GetIsPlayingIntro();
        string phase = GameManager.Instance.GetCurrentIntroPhase();
        
        if (isPlaying)
        {
            EditorGUILayout.HelpBox($"Intro Active — Phase: {phase}", MessageType.Info);
        }
        
        // Skip to specific phases
        EditorGUILayout.LabelField("Skip to Intro Phase", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Part 1\n(Dinner CG)"))
        {
            GameManager.Instance.SkipToIntroPart1();
            Repaint();
        }
        if (GUILayout.Button("Part 2\n(Reminisce CG)"))
        {
            GameManager.Instance.SkipToIntroPart2();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Simulation\n(Sex Gameplay)"))
        {
            GameManager.Instance.SkipToIntroSimulation();
            Repaint();
        }
        if (GUILayout.Button("Part 3\n(Bedroom CG)"))
        {
            GameManager.Instance.SkipToIntroPart3();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Part 4\n(Morning Live2D)"))
        {
            GameManager.Instance.SkipToIntroPart4();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Full skip button
        EditorGUILayout.LabelField("Skip Entire Intro", EditorStyles.miniBoldLabel);
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Light red
        if (GUILayout.Button("Skip Intro → Company 7:30 AM"))
        {
            GameManager.Instance.SkipIntroCG();
            Debug.Log("[DebugTools] Skipped intro CG sequence");
            Repaint();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();
    }

    private void DrawLocalizationControls()
    {
        EditorGUILayout.LabelField("Localization Controls", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to change the runtime locale.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        if (LocalizationManager.Instance == null)
        {
            EditorGUILayout.HelpBox("LocalizationManager not found.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField($"Current Language: {LocalizationManager.Instance.CurrentLanguage}");
        EditorGUILayout.LabelField($"Current Locale: {LocalizationManager.Instance.CurrentLocaleCode}");

        if (GameManager.Instance == null)
        {
            EditorGUILayout.HelpBox("GameManager not found. Locale changes will not update SettingsManager.", MessageType.Warning);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        DrawLocaleButton("en-US", Language.English);
        DrawLocaleButton("vi-VN", Language.Vietnamese);
        DrawLocaleButton("ja-JP", Language.Japanese);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Reload Current Locale CSVs"))
        {
            LocalizationManager.Instance.ReloadCsvLocalization(refresh: true, forceFromDiskInEditor: true);
            Debug.Log($"[DebugTools] Reloaded runtime localization CSVs for {LocalizationManager.Instance.CurrentLocaleCode}");
            Repaint();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLocaleButton(string localeCode, Language language)
    {
        bool isCurrentLocale = LocalizationManager.Instance != null &&
            LocalizationManager.Instance.CurrentLocaleCode == localeCode;

        using (new EditorGUI.DisabledScope(isCurrentLocale))
        {
            if (GUILayout.Button(localeCode))
                SetRuntimeLocale(localeCode, language);
        }
    }

    private void SetRuntimeLocale(string localeCode, Language language)
    {
        if (LocalizationManager.Instance == null)
            return;

        if (GameManager.Instance?.SettingsManager != null)
            GameManager.Instance.SettingsManager.UpdateLanguage(language);

        LocalizationManager.Instance.SetLanguage(language, refresh: true);
        Debug.Log($"[DebugTools] Changed runtime locale to {localeCode}");
        Repaint();
    }

    private void DrawCurrentStatus()
    {
        EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see status.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        
        if (GameManager.Instance == null)
        {
            EditorGUILayout.HelpBox("GameManager not found.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }
        
        DateTime time = GameManager.Instance.Time;
        EditorGUILayout.LabelField($"Time: {time:HH:mm} ({GameManager.Instance.Cycle})");
        EditorGUILayout.LabelField($"Area: {GameManager.Instance.Area}");
        EditorGUILayout.LabelField($"Day: {GameManager.Instance.DaysPlayed}");
        
        if (UIPanelManager.Instance != null)
        {
            EditorGUILayout.LabelField($"Section: {UIPanelManager.Instance.GetCurrentSection()}");
        }
        
        if (GameManager.Instance.GetIsPlayingIntro())
        {
            EditorGUILayout.LabelField($"Intro Phase: {GameManager.Instance.GetCurrentIntroPhase()}");
        }
        
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Refresh"))
        {
            Repaint();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void SkipToTime(int hour, int minute)
    {
        if (GameManager.Instance == null) return;
        
        DateTime currentTime = GameManager.Instance.Time;
        DateTime targetTime = currentTime.Date.AddHours(hour).AddMinutes(minute);
        
        if (targetTime <= currentTime)
        {
            targetTime = targetTime.AddDays(1);
        }
        
        TimeSpan duration = targetTime - currentTime;
        GameManager.Instance.AdvanceTimePublic(duration);
        
        Debug.Log($"[DebugTools] Skipped to {targetTime:HH:mm} (advanced {duration.TotalHours:F1} hours)");
        Repaint();
    }
    
    private void SkipHours(int hours)
    {
        if (GameManager.Instance == null) return;
        
        GameManager.Instance.AdvanceTimePublic(TimeSpan.FromHours(hours));
        
        Debug.Log($"[DebugTools] Skipped {hours} hour(s)");
        Repaint();
    }
    
    private void SkipToNextDay()
    {
        if (GameManager.Instance == null) return;
        
        DateTime currentTime = GameManager.Instance.Time;
        DateTime nextDay7AM = currentTime.Date.AddDays(1).AddHours(7);
        
        TimeSpan duration = nextDay7AM - currentTime;
        GameManager.Instance.AdvanceTimePublic(duration);
        
        Debug.Log($"[DebugTools] Skipped to next day at 7 AM");
        Repaint();
    }
    
    private void TeleportToArea(Area area)
    {
        if (GameManager.Instance == null) return;
        
        GameManager.Instance.ChangeArea((int)area);
        
        Debug.Log($"[DebugTools] Teleported to {area}");
        Repaint();
    }
}
