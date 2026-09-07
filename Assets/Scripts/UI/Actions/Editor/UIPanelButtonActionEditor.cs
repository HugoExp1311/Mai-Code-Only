using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Custom editor for UIPanelButtonAction component
/// Provides auto-fill button for Hide action to detect parent panel
/// </summary>
[CustomEditor(typeof(UIPanelButtonAction))]
[CanEditMultipleObjects]
public class UIPanelButtonActionEditor : Editor
{
    private SerializedProperty actionType;
    private SerializedProperty showPanelId;
    private SerializedProperty passData;
    private SerializedProperty showDelay;
    private SerializedProperty parentPanelBehavior;
    private SerializedProperty hidePanelId;
    private SerializedProperty hideCurrentPanel;
    private SerializedProperty chainHidePanelId;
    private SerializedProperty chainShowPanelId;
    private SerializedProperty chainPassData;
    private SerializedProperty targetSection;
    private SerializedProperty useSectionTransition;
    private SerializedProperty tabId;
    private SerializedProperty parentPanelId;
    private SerializedProperty soundType;
    private SerializedProperty specificArea;
    private SerializedProperty customActionDescription;
    private SerializedProperty dailyAction;
    private SerializedProperty actionIcons;
    private SerializedProperty confirmPopupPanelId;
    private SerializedProperty debugMode;
    
    private void OnEnable()
    {
        actionType = serializedObject.FindProperty("actionType");
        showPanelId = serializedObject.FindProperty("showPanelId");
        passData = serializedObject.FindProperty("passData");
        showDelay = serializedObject.FindProperty("showDelay");
        parentPanelBehavior = serializedObject.FindProperty("parentPanelBehavior");
        hidePanelId = serializedObject.FindProperty("hidePanelId");
        hideCurrentPanel = serializedObject.FindProperty("hideCurrentPanel");
        chainHidePanelId = serializedObject.FindProperty("chainHidePanelId");
        chainShowPanelId = serializedObject.FindProperty("chainShowPanelId");
        chainPassData = serializedObject.FindProperty("chainPassData");
        targetSection = serializedObject.FindProperty("targetSection");
        useSectionTransition = serializedObject.FindProperty("useSectionTransition");
        tabId = serializedObject.FindProperty("tabId");
        parentPanelId = serializedObject.FindProperty("parentPanelId");
        soundType = serializedObject.FindProperty("soundType");
        specificArea = serializedObject.FindProperty("specificArea");
        customActionDescription = serializedObject.FindProperty("customActionDescription");
        dailyAction = serializedObject.FindProperty("dailyAction");
        actionIcons = serializedObject.FindProperty("actionIcons");
        confirmPopupPanelId = serializedObject.FindProperty("confirmPopupPanelId");
        debugMode = serializedObject.FindProperty("debugMode");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        UIPanelButtonAction buttonAction = (UIPanelButtonAction)target;
        
        // Store previous action type before drawing the field
        int previousActionTypeIndex = actionType.enumValueIndex;
        // Action Configuration - header comes from [Header] attribute
        EditorGUILayout.PropertyField(actionType);
        
        // Update current action type after drawing
        UIPanelButtonAction.ActionType currentActionType = (UIPanelButtonAction.ActionType)actionType.enumValueIndex;
        
        // Auto-set sound type to Back when ActionType is Hide
        if (actionType.enumValueIndex != previousActionTypeIndex)
        {
            // Action type changed - update sound type if needed
            if (currentActionType == UIPanelButtonAction.ActionType.Hide)
            {
                soundType.enumValueIndex = (int)SoundType.Back;
            }
        }
        else if (currentActionType == UIPanelButtonAction.ActionType.Hide)
        {
            // Enforce Back sound for Hide actions (even if action type didn't change)
            if (soundType.enumValueIndex != (int)SoundType.Back)
            {
                soundType.enumValueIndex = (int)SoundType.Back;
            }
        }
        
        EditorGUILayout.Space();
        
        // Show Panel Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.Show || 
            currentActionType == UIPanelButtonAction.ActionType.Toggle)
        {
            EditorGUILayout.PropertyField(showPanelId);
            EditorGUILayout.PropertyField(passData);
            EditorGUILayout.PropertyField(showDelay);
            EditorGUILayout.PropertyField(parentPanelBehavior);
            EditorGUILayout.Space();
        }
        
        // Hide Panel Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.Hide || 
            currentActionType == UIPanelButtonAction.ActionType.Toggle)
        {
            EditorGUILayout.PropertyField(hideCurrentPanel);
            
            if (!hideCurrentPanel.boolValue)
            {
                EditorGUILayout.PropertyField(hidePanelId, 
                    new GUIContent("Hide Panel ID", 
                        "Panel ID to hide. Leave empty to auto-detect parent panel at runtime."));
                
                // Auto-fill options for parent panel detection
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Auto-Detect Options:", EditorStyles.miniLabel);
                
                // Find different panel options
                string directParentId = FindDirectParentPanelId(buttonAction.transform);
                string rootParentId = FindRootParentPanelId(buttonAction.transform);
                string currentPanelId = FindCurrentPanelId(buttonAction.transform);
                
                EditorGUILayout.BeginHorizontal();
                
                // Direct parent (immediate parent)
                if (!string.IsNullOrEmpty(directParentId))
                {
                    if (GUILayout.Button($"Direct Parent: {directParentId}", GUILayout.Height(20)))
                    {
                        hidePanelId.stringValue = directParentId;
                        EditorUtility.SetDirty(target);
                    }
                }
                
                // Root parent (skip sub-panels, find main panel)
                if (!string.IsNullOrEmpty(rootParentId) && rootParentId != directParentId)
                {
                    if (GUILayout.Button($"Root Parent: {rootParentId}", GUILayout.Height(20)))
                    {
                        hidePanelId.stringValue = rootParentId;
                        EditorUtility.SetDirty(target);
                    }
                }
                
                // Current panel (close self)
                if (!string.IsNullOrEmpty(currentPanelId))
                {
                    if (GUILayout.Button($"Current Panel: {currentPanelId}", GUILayout.Height(20)))
                    {
                        hidePanelId.stringValue = currentPanelId;
                        EditorUtility.SetDirty(target);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Show info about detected panels
                if (!string.IsNullOrEmpty(directParentId) || !string.IsNullOrEmpty(rootParentId) || !string.IsNullOrEmpty(currentPanelId))
                {
                    string info = "";
                    if (!string.IsNullOrEmpty(currentPanelId))
                        info += $"Current: {currentPanelId}  ";
                    if (!string.IsNullOrEmpty(directParentId))
                        info += $"Parent: {directParentId}  ";
                    if (!string.IsNullOrEmpty(rootParentId) && rootParentId != directParentId)
                        info += $"Root: {rootParentId}";
                    
                    EditorGUILayout.HelpBox(info.Trim(), MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "No UIPanel found in hierarchy. Leave empty to auto-detect at runtime.",
                        MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
        }
        
        // Chain Action Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.Chain)
        {
            EditorGUILayout.PropertyField(chainHidePanelId, 
                new GUIContent("Hide Panel ID", 
                    "Panel to hide first. Leave empty to auto-detect parent panel."));
            
            // Auto-fill options for chain hide panel
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Auto-Detect Hide Panel:", EditorStyles.miniLabel);
            
            string chainDirectParent = FindDirectParentPanelId(buttonAction.transform);
            string chainRootParent = FindRootParentPanelId(buttonAction.transform);
            string chainCurrent = FindCurrentPanelId(buttonAction.transform);
            
            EditorGUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(chainDirectParent))
            {
                if (GUILayout.Button($"Hide: {chainDirectParent}", GUILayout.Height(20)))
                {
                    chainHidePanelId.stringValue = chainDirectParent;
                    EditorUtility.SetDirty(target);
                }
            }
            if (!string.IsNullOrEmpty(chainRootParent) && chainRootParent != chainDirectParent)
            {
                if (GUILayout.Button($"Hide Root: {chainRootParent}", GUILayout.Height(20)))
                {
                    chainHidePanelId.stringValue = chainRootParent;
                    EditorUtility.SetDirty(target);
                }
            }
            if (!string.IsNullOrEmpty(chainCurrent))
            {
                if (GUILayout.Button($"Hide Current: {chainCurrent}", GUILayout.Height(20)))
                {
                    chainHidePanelId.stringValue = chainCurrent;
                    EditorUtility.SetDirty(target);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.PropertyField(chainShowPanelId, 
                new GUIContent("Show Panel ID", 
                    "Panel to show after hiding. Required for chain action."));
            EditorGUILayout.PropertyField(chainPassData);
            EditorGUILayout.Space();
        }
        
        // Section Switch Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.SwitchSection)
        {
            EditorGUILayout.PropertyField(targetSection, 
                new GUIContent("Target Section", 
                    "Game section to switch to when button is clicked"));
            EditorGUILayout.PropertyField(useSectionTransition, 
                new GUIContent("Use Transition", 
                    "Use transition panel animation when switching sections"));
            EditorGUILayout.Space();
        }
        
        // Tab Switch Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.SwitchTab)
        {
            EditorGUILayout.PropertyField(tabId, 
                new GUIContent("Tab ID", 
                    "ID of the tab to switch to (e.g., 'Profile', 'Settings')"));
            
            EditorGUILayout.PropertyField(parentPanelId, 
                new GUIContent("Parent Panel ID", 
                    "Panel ID containing the tabs. Leave empty to auto-detect parent panel at runtime."));
            
            // Auto-fill options for parent panel detection
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Auto-Detect Options:", EditorStyles.miniLabel);
            
            string directParentId = FindDirectParentPanelId(buttonAction.transform);
            string rootParentId = FindRootParentPanelId(buttonAction.transform);
            
            EditorGUILayout.BeginHorizontal();
            
            // Direct parent (immediate parent)
            if (!string.IsNullOrEmpty(directParentId))
            {
                if (GUILayout.Button($"Direct Parent: {directParentId}", GUILayout.Height(20)))
                {
                    parentPanelId.stringValue = directParentId;
                    EditorUtility.SetDirty(target);
                }
            }
            
            // Root parent (skip sub-panels, find main panel)
            if (!string.IsNullOrEmpty(rootParentId) && rootParentId != directParentId)
            {
                if (GUILayout.Button($"Root Parent: {rootParentId}", GUILayout.Height(20)))
                {
                    parentPanelId.stringValue = rootParentId;
                    EditorUtility.SetDirty(target);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Show info about detected panels
            if (!string.IsNullOrEmpty(directParentId) || !string.IsNullOrEmpty(rootParentId))
            {
                string info = "";
                if (!string.IsNullOrEmpty(directParentId))
                    info += $"Parent: {directParentId}  ";
                if (!string.IsNullOrEmpty(rootParentId) && rootParentId != directParentId)
                    info += $"Root: {rootParentId}";
                
                EditorGUILayout.HelpBox(info.Trim(), MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No UIPanel found in hierarchy. Leave empty to auto-detect at runtime.",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        // Change Area Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.ChangeArea)
        {
            EditorGUILayout.PropertyField(specificArea, 
                new GUIContent("Target Area", 
                    "Area to change to when button is clicked"));
            EditorGUILayout.PropertyField(hideCurrentPanel, 
                new GUIContent("Hide Current Panel", 
                    "If enabled, hides the current panel after successful area change. Otherwise, tries to hide parent panel."));
            EditorGUILayout.Space();
        }
        
        // Show Confirm Popup Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.ShowConfirmPopup)
        {
            EditorGUILayout.PropertyField(dailyAction, 
                new GUIContent("Daily Action", 
                    "Select which daily action to trigger (Sleep, Eating, Sex)"));
            
            EditorGUILayout.PropertyField(actionIcons, 
                new GUIContent("Action Icons", 
                    "Icon sprites for popup states (optional - overrides database icons)\n" +
                    "Index 0 = Initial state, Index 1 = Secondary state (e.g., Eating with Mai)"),
                true); // true = include children (array elements)
            
            EditorGUILayout.PropertyField(confirmPopupPanelId, 
                new GUIContent("Popup Panel ID", 
                    "Panel ID for the Action Confirm Popup (default: 'Action Confirm Popup')"));
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Popup content (header, buttons, text) is configured in ActionConfirmPopupDatabase.cs\n" +
                "Icons: [0] = Initial state, [1] = Secondary state (for Eating), etc.\n" +
                "Leave empty to use database defaults.",
                MessageType.Info);
            
            EditorGUILayout.Space();
        }
        
        // Custom Action Settings - header comes from [Header] attribute
        if (currentActionType == UIPanelButtonAction.ActionType.Custom)
        {
            // Auto-detect custom action information
            CustomActionInfo actionInfo = DetectCustomAction(buttonAction);
            
            if (actionInfo != null && !string.IsNullOrEmpty(actionInfo.MethodName))
            {
                // Show auto-detected custom action information
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Custom Action Detected:", EditorStyles.boldLabel);
                
                string methodInfo = $"Will call: `{actionInfo.MethodName}()`";
                if (!string.IsNullOrEmpty(actionInfo.ScriptName))
                {
                    methodInfo += $" in `{actionInfo.ScriptName}`";
                }
                
                EditorGUILayout.LabelField(methodInfo, EditorStyles.wordWrappedLabel);
                
                if (!string.IsNullOrEmpty(actionInfo.AdditionalInfo))
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField(actionInfo.AdditionalInfo, EditorStyles.wordWrappedMiniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                // Try to detect from Button's onClick persistent calls
                List<ButtonMethodInfo> buttonMethods = InspectButtonOnClick(buttonAction);
                
                if (buttonMethods != null && buttonMethods.Count > 0)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Custom Action Detected (from Button onClick):", EditorStyles.boldLabel);
                    
                    foreach (var methodInfo in buttonMethods)
                    {
                        string info = $"Will call: `{methodInfo.MethodName}()`";
                        if (!string.IsNullOrEmpty(methodInfo.ScriptName))
                        {
                            info += $" in `{methodInfo.ScriptName}`";
                        }
                        if (!string.IsNullOrEmpty(methodInfo.TargetObjectName))
                        {
                            info += $" on `{methodInfo.TargetObjectName}`";
                        }
                        
                        EditorGUILayout.LabelField(info, EditorStyles.wordWrappedLabel);
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    // No custom handler detected
                    EditorGUILayout.HelpBox(
                        "No custom action handler detected. " +
                        "Make sure the button is referenced in a script (e.g., MaiPanel) that subscribes to its onClick event.",
                        MessageType.Warning);
                }
            }
            
            EditorGUILayout.Space();
        }
        
        // Sound Settings - header comes from [Header] attribute
        // Show info and disable sound type when ActionType is Hide
        if (currentActionType == UIPanelButtonAction.ActionType.Hide)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(soundType, 
                new GUIContent("Sound Type", 
                    "Automatically set to Back for Hide actions."));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox("Sound type is automatically set to 'Back' for Hide actions.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.PropertyField(soundType);
        }
        
        // Show specificArea field for Place sound type or ChangeArea action type
        if ((SoundType)soundType.enumValueIndex == SoundType.Place)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(specificArea, 
                new GUIContent("Specific Area", 
                    "Area for place-specific sound"));
            EditorGUI.indentLevel--;
        }
        else if (currentActionType == UIPanelButtonAction.ActionType.ChangeArea)
        {
            // specificArea is already shown in ChangeArea settings section above
            // Don't show it again here
        }
        
        EditorGUILayout.Space();
        
        // Debug - header comes from [Header] attribute
        EditorGUILayout.PropertyField(debugMode);
        
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// Find direct parent panel (first UIPanel found up the hierarchy)
    /// </summary>
    private string FindDirectParentPanelId(Transform startTransform)
    {
        Transform current = startTransform.parent;
        
        while (current != null)
        {
            var panel = current.GetComponent<UIPanel>();
            if (panel != null)
            {
                return panel.PanelId;
            }
            
            current = current.parent;
        }
        
        return null;
    }
    
    /// <summary>
    /// Find root parent panel (the topmost panel in hierarchy)
    /// Useful for nested hierarchies like: SkillPanel → SkillProfilePanel → Button
    /// Returns the highest panel in the hierarchy chain
    /// </summary>
    private string FindRootParentPanelId(Transform startTransform)
    {
        Transform current = startTransform.parent;
        UIPanel topmostPanel = null;
        
        // Collect all panels in hierarchy
        List<UIPanel> panelsInHierarchy = new List<UIPanel>();
        
        while (current != null)
        {
            var panel = current.GetComponent<UIPanel>();
            if (panel != null)
            {
                panelsInHierarchy.Add(panel);
                topmostPanel = panel; // This will be the last one found (topmost)
            }
            
            current = current.parent;
        }
        
        // Return topmost panel (the root/main panel, not a sub-panel)
        return topmostPanel != null ? topmostPanel.PanelId : null;
    }
    
    /// <summary>
    /// Find current panel (panel that this button is directly inside)
    /// </summary>
    private string FindCurrentPanelId(Transform startTransform)
    {
        // Check if button itself is on a panel (unlikely but possible)
        var panel = startTransform.GetComponent<UIPanel>();
        if (panel != null)
        {
            return panel.PanelId;
        }
        
        // Otherwise find first parent panel
        return FindDirectParentPanelId(startTransform);
    }
    
    /// <summary>
    /// Information about a custom action
    /// </summary>
    private class CustomActionInfo
    {
        public string MethodName { get; set; }
        public string ScriptName { get; set; }
        public string AdditionalInfo { get; set; }
    }
    
    /// <summary>
    /// Information about a method called from Button onClick
    /// </summary>
    private class ButtonMethodInfo
    {
        public string MethodName { get; set; }
        public string ScriptName { get; set; }
        public string TargetObjectName { get; set; }
    }
    
    /// <summary>
    /// Detect custom action by checking if any MaiPanel references this UIPanelButtonAction
    /// </summary>
    private CustomActionInfo DetectCustomAction(UIPanelButtonAction buttonAction)
    {
        // Find all MaiPanel components in the project (including prefabs)
        MaiPanel[] allMaiPanels = Resources.FindObjectsOfTypeAll<MaiPanel>();
        
        foreach (MaiPanel maiPanel in allMaiPanels)
        {
            if (maiPanel == null)
                continue;
            
            // Use reflection to check if this buttonAction is referenced in any of MaiPanel's fields
            SerializedObject maiPanelSO = new SerializedObject(maiPanel);
            SerializedProperty maiSpriteProp = maiPanelSO.FindProperty("maiSpriteButtonAction");
            SerializedProperty dialogueProp = maiPanelSO.FindProperty("dialogueButtonAction");
            SerializedProperty giftProp = maiPanelSO.FindProperty("giftButtonAction");
            
            // Check if this buttonAction matches any of the MaiPanel's button references
            if (maiSpriteProp != null && maiSpriteProp.objectReferenceValue == buttonAction)
            {
                return new CustomActionInfo
                {
                    MethodName = "OnMaiSpriteClick",
                    ScriptName = "MaiPanel.cs",
                    AdditionalInfo = "Enables Dialogue and Gift button GameObjects"
                };
            }
            
            if (dialogueProp != null && dialogueProp.objectReferenceValue == buttonAction)
            {
                return new CustomActionInfo
                {
                    MethodName = "OnDialogueButtonClick",
                    ScriptName = "MaiPanel.cs",
                    AdditionalInfo = "Calls CharacterInteractManager.InitiateTalk() and disables interaction buttons"
                };
            }
            
            if (giftProp != null && giftProp.objectReferenceValue == buttonAction)
            {
                return new CustomActionInfo
                {
                    MethodName = "OnGiftButtonClick",
                    ScriptName = "MaiPanel.cs",
                    AdditionalInfo = "Calls CharacterInteractManager.InitiateGift() and disables interaction buttons"
                };
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Inspect Button's onClick UnityEvent to find subscribed methods
    /// </summary>
    private List<ButtonMethodInfo> InspectButtonOnClick(UIPanelButtonAction buttonAction)
    {
        Button button = buttonAction.GetComponent<Button>();
        if (button == null)
            return null;
        
        List<ButtonMethodInfo> methods = new List<ButtonMethodInfo>();
        
        // Get the onClick UnityEvent
        SerializedObject buttonSO = new SerializedObject(button);
        SerializedProperty onClickProp = buttonSO.FindProperty("m_OnClick");
        
        if (onClickProp != null)
        {
            // Get the persistent calls
            SerializedProperty callsProp = onClickProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            if (callsProp != null && callsProp.isArray)
            {
                for (int i = 0; i < callsProp.arraySize; i++)
                {
                    SerializedProperty callProp = callsProp.GetArrayElementAtIndex(i);
                    
                    SerializedProperty targetProp = callProp.FindPropertyRelative("m_Target");
                    SerializedProperty methodNameProp = callProp.FindPropertyRelative("m_MethodName");
                    
                    if (targetProp != null && methodNameProp != null)
                    {
                        UnityEngine.Object target = targetProp.objectReferenceValue;
                        string methodName = methodNameProp.stringValue;
                        
                        if (target != null && !string.IsNullOrEmpty(methodName))
                        {
                            // Get script name from the target object
                            string scriptName = target.GetType().Name + ".cs";
                            string targetObjectName = target.name;
                            
                            methods.Add(new ButtonMethodInfo
                            {
                                MethodName = methodName,
                                ScriptName = scriptName,
                                TargetObjectName = targetObjectName
                            });
                        }
                    }
                }
            }
        }
        
        return methods.Count > 0 ? methods : null;
    }
}

