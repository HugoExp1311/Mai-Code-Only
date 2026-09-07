using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Post-processor that automatically adds UIPanelButtonAction to Button components
/// Analyzes button context and auto-configures actions based on hierarchy
/// </summary>
public class ButtonActionPostProcessor
{
    /// <summary>
    /// Process a GameObject and add UIPanelButtonAction to all Buttons that need it
    /// </summary>
    public static void ProcessGameObject(GameObject root, bool isPrefab = false)
    {
        if (!UIMigrationPreferences.EnableAutoButtonSetup)
            return;
        
        List<Button> buttons = new List<Button>();
        CollectButtons(root.transform, buttons);
        
        int processedCount = 0;
        
        foreach (Button button in buttons)
        {
            if (ProcessButton(button, isPrefab))
            {
                processedCount++;
            }
        }
        
        if (processedCount > 0 && UIMigrationPreferences.LogActions)
        {
            Debug.Log($"[ButtonActionPostProcessor] Processed {processedCount} buttons on '{root.name}'");
        }
    }
    
    /// <summary>
    /// Process a single Button component
    /// </summary>
    private static bool ProcessButton(Button button, bool isPrefab)
    {
        if (button == null)
            return false;
        
        // Check if button already has UIPanelButtonAction
        var existingAction = button.GetComponent<UIPanelButtonAction>();
        if (existingAction != null)
            return false; // Already has action component
        
        // Add UIPanelButtonAction component
        var actionComponent = button.gameObject.AddComponent<UIPanelButtonAction>();
        SerializedObject actionSO = new SerializedObject(actionComponent);
        
        // Analyze button context to auto-configure
        AnalyzeAndConfigureButton(button, actionSO);
        
        actionSO.ApplyModifiedProperties();
        
        // Mark as dirty
        EditorUtility.SetDirty(button.gameObject);
        if (!isPrefab)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(button.gameObject.scene);
        }
        
        if (UIMigrationPreferences.LogActions)
        {
            Debug.Log($"[ButtonActionPostProcessor] Added UIPanelButtonAction to button '{button.gameObject.name}'");
        }
        
        return true;
    }
    
    /// <summary>
    /// Analyze button context and auto-configure UIPanelButtonAction
    /// </summary>
    private static void AnalyzeAndConfigureButton(Button button, SerializedObject actionSO)
    {
        string buttonName = button.gameObject.name.ToLower();
        Transform parent = button.transform.parent;
        
        // Try to find parent UIPanel
        UIPanel parentPanel = FindParentPanel2(button.transform);
        
        // Determine action type based on button name patterns
        UIPanelButtonAction.ActionType actionType = DetermineActionType(buttonName, parentPanel);
        actionSO.FindProperty("actionType").enumValueIndex = (int)actionType;
        
        // Configure based on action type
        switch (actionType)
        {
            case UIPanelButtonAction.ActionType.Show:
                ConfigureShowAction(button, actionSO, buttonName);
                break;
                
            case UIPanelButtonAction.ActionType.Hide:
                ConfigureHideAction(button, actionSO, parentPanel);
                break;
                
            case UIPanelButtonAction.ActionType.Chain:
                ConfigureChainAction(button, actionSO, buttonName);
                break;
                
            default:
                // Default to Show with button name as panel ID
                ConfigureShowAction(button, actionSO, buttonName);
                break;
        }
        
        // Determine sound type based on button name
        SoundType soundType = DetermineSoundType(buttonName);
        actionSO.FindProperty("soundType").enumValueIndex = (int)soundType;
    }
    
    /// <summary>
    /// Determine action type based on button name and context
    /// </summary>
    private static UIPanelButtonAction.ActionType DetermineActionType(string buttonName, UIPanel parentPanel)
    {
        // Check for hide/close patterns
        if (buttonName.Contains("close") || buttonName.Contains("hide") || 
            buttonName.Contains("back") || buttonName.Contains("cancel"))
        {
            return UIPanelButtonAction.ActionType.Hide;
        }
        
        // Check for chain patterns (transition buttons)
        if (buttonName.Contains("next") || buttonName.Contains("continue") || 
            buttonName.Contains("to") || buttonName.Contains("goto"))
        {
            return UIPanelButtonAction.ActionType.Chain;
        }
        
        // Default to Show
        return UIPanelButtonAction.ActionType.Show;
    }
    
    /// <summary>
    /// Configure Show action
    /// </summary>
    private static void ConfigureShowAction(Button button, SerializedObject actionSO, string buttonName)
    {
        // Try to extract panel ID from button name
        string panelId = ExtractPanelIdFromName(buttonName);
        
        if (!string.IsNullOrEmpty(panelId))
        {
            actionSO.FindProperty("showPanelId").stringValue = panelId;
        }
        else
        {
            // Default: use button name as panel ID
            actionSO.FindProperty("showPanelId").stringValue = button.gameObject.name;
        }
    }
    
    /// <summary>
    /// Configure Hide action
    /// </summary>
    private static void ConfigureHideAction(Button button, SerializedObject actionSO, UIPanel parentPanel)
    {
        if (parentPanel != null)
        {
            // Auto-detect parent panel ID
            actionSO.FindProperty("hidePanelId").stringValue = parentPanel.PanelId;
            actionSO.FindProperty("hideCurrentPanel").boolValue = false;
        }
        else
        {
            // Leave empty - will auto-detect at runtime
            actionSO.FindProperty("hidePanelId").stringValue = "";
            actionSO.FindProperty("hideCurrentPanel").boolValue = true;
        }
    }
    
    /// <summary>
    /// Configure Chain action
    /// </summary>
    private static void ConfigureChainAction(Button button, SerializedObject actionSO, string buttonName)
    {
        UIPanel parentPanel = FindParentPanel2(button.transform);
        
        if (parentPanel != null)
        {
            actionSO.FindProperty("chainHidePanelId").stringValue = parentPanel.PanelId;
        }
        
        // Try to extract target panel from button name
        string targetPanelId = ExtractPanelIdFromName(buttonName);
        if (!string.IsNullOrEmpty(targetPanelId))
        {
            actionSO.FindProperty("chainShowPanelId").stringValue = targetPanelId;
        }
    }
    
    /// <summary>
    /// Determine sound type based on button name patterns
    /// </summary>
    private static SoundType DetermineSoundType(string buttonName)
    {
        if (buttonName.Contains("back") || buttonName.Contains("close") || buttonName.Contains("cancel"))
        {
            return SoundType.Back;
        }
        
        if (buttonName.Contains("action") || buttonName.Contains("confirm") || buttonName.Contains("ok"))
        {
            return SoundType.Action;
        }
        
        // Default to normal
        return SoundType.Normal;
    }
    
    /// <summary>
    /// Find parent UIPanel component by traversing up hierarchy
    /// </summary>
    private static UIPanel FindParentPanel2(Transform start)
    {
        Transform current = start.parent;
        
        while (current != null)
        {
            UIPanel panel = current.GetComponent<UIPanel>();
            if (panel != null)
            {
                return panel;
            }
            
            current = current.parent;
        }
        
        return null;
    }
    
    /// <summary>
    /// Extract panel ID from button name (simple heuristic)
    /// </summary>
    private static string ExtractPanelIdFromName(string buttonName)
    {
        // Remove common prefixes
        string id = buttonName
            .Replace("button", "")
            .Replace("btn", "")
            .Replace("show", "")
            .Replace("open", "")
            .Replace("goto", "")
            .Replace("to", "")
            .Replace("_", "")
            .Trim();
        
        // Capitalize first letter
        if (!string.IsNullOrEmpty(id) && id.Length > 0)
        {
            id = char.ToUpper(id[0]) + (id.Length > 1 ? id.Substring(1) : "");
        }
        
        return id;
    }
    
    /// <summary>
    /// Collect all Button components recursively
    /// </summary>
    private static void CollectButtons(Transform root, List<Button> collection)
    {
        Button button = root.GetComponent<Button>();
        if (button != null)
        {
            collection.Add(button);
        }
        
        foreach (Transform child in root)
        {
            CollectButtons(child, collection);
        }
    }
    
}

