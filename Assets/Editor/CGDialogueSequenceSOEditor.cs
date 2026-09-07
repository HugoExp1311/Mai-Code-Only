using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using TMPro;
using Base.CG;
using Base.Dialogues;
using Base.Character.Stats;
using Base.Localization;
using Base.Localization.EditorTools;

[CustomEditor(typeof(CGDialogueSequenceSO))]
public class CGDialogueSequenceSOEditor : Editor
{
    private CGDialogueSequenceSO _sequenceSO;
    private SerializedProperty _sequenceIDProp;
    private SerializedProperty _startingNodeIDProp;
    private SerializedProperty _rewardsOnCompletionProp;
    private SerializedProperty _nodeListProp;

    private Dictionary<string, bool> _nodeFoldouts = new Dictionary<string, bool>();

    private GUIStyle _richBoldFoldoutStyle;
    private GUIStyle _richMiniLabelStyle;
    private GUIStyle _nodeIDLabelStyle;
    private GUIStyle _clickableLinkStyle;

    private Vector2 _nodeListScrollPosition;

    private string _selectedLocaleCode = "en-US";

    // Navigation improvements
    private string _searchFilter = "";
    private Dictionary<string, List<string>> _incomingReferencesCache;
    private bool _showIncomingReferences = false;

    private void OnEnable()
    {
        _sequenceSO = (CGDialogueSequenceSO)target;
        _sequenceIDProp = serializedObject.FindProperty("sequenceID");
        _startingNodeIDProp = serializedObject.FindProperty("startingNodeID");
        _rewardsOnCompletionProp = serializedObject.FindProperty("rewardsOnCompletion");
        _nodeListProp = serializedObject.FindProperty("nodeList");
        _nodeFoldouts.Clear();

        _incomingReferencesCache = null;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (_richBoldFoldoutStyle == null)
            _richBoldFoldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, richText = true };
        if (_richMiniLabelStyle == null)
            _richMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true, wordWrap = true };
        if (_nodeIDLabelStyle == null)
            _nodeIDLabelStyle = new GUIStyle(EditorStyles.label) { richText = true, fontStyle = FontStyle.Bold };
        if (_clickableLinkStyle == null)
        {
            _clickableLinkStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.75f, 0.95f) : Color.blue }
            };
            _clickableLinkStyle.hover.textColor = Color.Lerp(_clickableLinkStyle.normal.textColor, Color.white, 0.5f);
        }

        EditorGUILayout.PropertyField(_sequenceIDProp);
        EditorGUILayout.PropertyField(_startingNodeIDProp);

        if (!string.IsNullOrEmpty(_startingNodeIDProp.stringValue) && _sequenceSO.GetNodeByID(_startingNodeIDProp.stringValue) == null)
        {
            EditorGUILayout.HelpBox($"Warning: Starting Node ID '{_startingNodeIDProp.stringValue}' not found in the Node List.", MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General Sequence Completion Rewards", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_rewardsOnCompletionProp, true);

        EditorGUILayout.Space();

        // Navigation toolbar
        EditorGUILayout.BeginHorizontal("toolbar");

        // Expand/Collapse All buttons
        if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            ExpandAllNodes();
        }
        if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            CollapseAllNodes();
        }

        GUILayout.Space(10);

        // Search filter
        GUILayout.Label("Search:", GUILayout.Width(50));
        string newSearchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
        if (newSearchFilter != _searchFilter)
        {
            _searchFilter = newSearchFilter;
            Repaint();
        }

        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            _searchFilter = "";
            GUI.FocusControl(null);
            Repaint();
        }

        GUILayout.Space(10);

        // Show incoming references toggle
        bool newShowIncoming = GUILayout.Toggle(_showIncomingReferences, "Show Incoming", EditorStyles.toolbarButton, GUILayout.Width(100));
        if (newShowIncoming != _showIncomingReferences)
        {
            _showIncomingReferences = newShowIncoming;
            if (_showIncomingReferences && _incomingReferencesCache == null)
            {
                BuildIncomingReferencesCache();
            }
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Node List (" + _nodeListProp.arraySize + ")", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));

        int selectedLocaleIndex = Array.IndexOf(RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes, _selectedLocaleCode);
        if (selectedLocaleIndex < 0)
            selectedLocaleIndex = 0;

        int newSelectedLocaleIndex = EditorGUILayout.Popup(
            selectedLocaleIndex,
            RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes,
            GUILayout.ExpandWidth(true));

        if (newSelectedLocaleIndex != selectedLocaleIndex)
            _selectedLocaleCode = RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes[newSelectedLocaleIndex];

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
        _nodeListScrollPosition = EditorGUILayout.BeginScrollView(_nodeListScrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (_nodeListProp.arraySize == 0)
        {
            EditorGUILayout.LabelField("   (No nodes defined in this sequence)");
        }
        else
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < _nodeListProp.arraySize; i++)
            {
                SerializedProperty nodeElementProp = _nodeListProp.GetArrayElementAtIndex(i);
                SerializedProperty nodeIDProp = nodeElementProp.FindPropertyRelative("nodeID");
                SerializedProperty linesProp = nodeElementProp.FindPropertyRelative("lines");
                SerializedProperty choicesProp = nodeElementProp.FindPropertyRelative("choices");
                SerializedProperty nodeRewardsProp = nodeElementProp.FindPropertyRelative("rewardsOnNodeCompletion");

                string nodeIDVal = nodeIDProp.stringValue;
                string uniqueFoldoutKey = $"{_sequenceSO.GetInstanceID()}_{nodeIDVal}_{i}";

                string displayNodeID = string.IsNullOrEmpty(nodeIDVal) ? $"Node_{i}_(UNASSIGNED_ID)" : nodeIDVal;

                if (!_nodeFoldouts.ContainsKey(uniqueFoldoutKey))
                    _nodeFoldouts[uniqueFoldoutKey] = false;

                // Search filter
                bool matchesSearch = string.IsNullOrEmpty(_searchFilter) ||
                                    nodeIDVal.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    NodeContainsSearchText(nodeElementProp, _searchFilter);

                if (!matchesSearch)
                    continue;

                EditorGUILayout.BeginHorizontal();
                string nodeLabelColor = EditorGUIUtility.isProSkin ? "white" : "black";
                string nodePrefix = "";

                if (nodeIDVal == _startingNodeIDProp.stringValue)
                {
                    nodeLabelColor = "green";
                    nodePrefix = "➔ ";
                }
                else if (IsNodeReferencedByChoice(nodeIDVal, _sequenceSO))
                {
                    nodeLabelColor = "cyan";
                }

                if (GUILayout.Button(new GUIContent($"<color={nodeLabelColor}><b>{nodePrefix}{displayNodeID}{(IsNodeReferencedByChoice(nodeIDVal, _sequenceSO) && !(nodeIDVal == _startingNodeIDProp.stringValue) ? " (Targeted)" : "")}</b></color>"), _nodeIDLabelStyle, GUILayout.ExpandWidth(false)))
                {
                    _nodeFoldouts[uniqueFoldoutKey] = !_nodeFoldouts[uniqueFoldoutKey];
                }
                _nodeFoldouts[uniqueFoldoutKey] = EditorGUILayout.Foldout(_nodeFoldouts[uniqueFoldoutKey], GUIContent.none, true, EditorStyles.foldout);
                EditorGUILayout.EndHorizontal();

                if (_nodeFoldouts[uniqueFoldoutKey])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(nodeIDProp, new GUIContent("Node ID"));

                    // Show incoming references if enabled
                    if (_showIncomingReferences && _incomingReferencesCache != null && _incomingReferencesCache.ContainsKey(nodeIDVal))
                    {
                        var incomingRefs = _incomingReferencesCache[nodeIDVal];
                        if (incomingRefs.Count > 0)
                        {
                            EditorGUILayout.LabelField($"⬅ Incoming from: {string.Join(", ", incomingRefs)}", _richMiniLabelStyle);
                        }
                    }

                    EditorGUILayout.LabelField($"Lines ({linesProp.arraySize})", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    if (linesProp.arraySize == 0) EditorGUILayout.LabelField("(No lines)", EditorStyles.miniLabel);
                    for (int j = 0; j < linesProp.arraySize; j++)
                    {
                        SerializedProperty lineElementProp = linesProp.GetArrayElementAtIndex(j);
                        SerializedProperty speakerProp = lineElementProp.FindPropertyRelative("speaker");
                        SerializedProperty localizationKeyProp = lineElementProp.FindPropertyRelative("localizationKey");
                        SerializedProperty cgSpriteProp = lineElementProp.FindPropertyRelative("cgSprite");

                        // Display speaker on its own line
                        EditorGUILayout.LabelField($"<b>Speaker:</b> {(RewardTarget)speakerProp.enumValueIndex}", _richMiniLabelStyle);

                        // Display localized dialogue
                        EditorGUILayout.LabelField("<b>Runtime CSV Dialogue:</b>", _richMiniLabelStyle);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(localizationKeyProp, new GUIContent("Localization Key"));
                        DisplayRuntimeCsvPreview(localizationKeyProp.stringValue);
                        EditorGUI.indentLevel--;

                        // Show CG sprite with aspect ratio preserved
                        Sprite cgSprite = cgSpriteProp.objectReferenceValue as Sprite;
                        if (cgSprite != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(EditorGUI.indentLevel * 15);

                            // Calculate preview size maintaining aspect ratio
                            float maxWidth = 150f;
                            float maxHeight = 150f;
                            float spriteWidth = cgSprite.texture.width;
                            float spriteHeight = cgSprite.texture.height;
                            float aspectRatio = spriteWidth / spriteHeight;

                            float previewWidth, previewHeight;
                            if (aspectRatio > 1) // Wider than tall
                            {
                                previewWidth = maxWidth;
                                previewHeight = maxWidth / aspectRatio;
                            }
                            else // Taller than wide or square
                            {
                                previewHeight = maxHeight;
                                previewWidth = maxHeight * aspectRatio;
                            }

                            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.Width(previewWidth), GUILayout.Height(previewHeight));
                            EditorGUI.DrawPreviewTexture(previewRect, cgSprite.texture);

                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.LabelField($"   🖼️ CG Sprite: {cgSprite.name}", _richMiniLabelStyle);
                            EditorGUILayout.LabelField($"   Size: {cgSprite.texture.width}x{cgSprite.texture.height}", _richMiniLabelStyle);
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.LabelField("   🖼️ No CG Sprite", _richMiniLabelStyle);
                        }

                        // Show font style information
                        var fontStyleProp = lineElementProp.FindPropertyRelative("fontStyle");
                        if (fontStyleProp != null)
                        {
                            FontStyles fontStyleValue = (FontStyles)fontStyleProp.intValue;
                            if (fontStyleValue != FontStyles.Normal)
                            {
                                EditorGUILayout.LabelField($"   ✏️ <b>Style:</b> {fontStyleValue}", _richMiniLabelStyle);
                            }
                        }

                        // Show monologue indicator
                        var isMonologueProp = lineElementProp.FindPropertyRelative("isMonologue");
                        if (isMonologueProp != null && isMonologueProp.boolValue)
                        {
                            EditorGUILayout.LabelField("   💭 <b>Monologue</b>", _richMiniLabelStyle);
                        }
                    }
                    EditorGUI.indentLevel--;

                    EditorGUILayout.LabelField($"Choices ({choicesProp.arraySize})", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    if (choicesProp.arraySize == 0) EditorGUILayout.LabelField("(No choices - ends path)", EditorStyles.miniLabel);
                    for (int k = 0; k < choicesProp.arraySize; k++)
                    {
                        SerializedProperty choiceElementProp = choicesProp.GetArrayElementAtIndex(k);
                        SerializedProperty localizationKeyProp = choiceElementProp.FindPropertyRelative("localizationKey");
                        SerializedProperty nextNodeID_choiceProp = choiceElementProp.FindPropertyRelative("nextNodeID");
                        SerializedProperty nextSeqSO_choiceProp = choiceElementProp.FindPropertyRelative("nextSequenceSO");
                        SerializedProperty startInNextSeqID_choiceProp = choiceElementProp.FindPropertyRelative("startingNodeInNextSequenceID");

                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"<b>Choice {k + 1}:</b>", _richMiniLabelStyle);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField("<b>Runtime CSV Choice Text:</b>", _richMiniLabelStyle);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(localizationKeyProp, new GUIContent("Localization Key"));
                        DisplayRuntimeCsvPreview(localizationKeyProp.stringValue);
                        EditorGUI.indentLevel--;
                        EditorGUILayout.EndVertical();

                        if (nextSeqSO_choiceProp.objectReferenceValue != null)
                        {
                            CGDialogueSequenceSO targetSeq = (CGDialogueSequenceSO)nextSeqSO_choiceProp.objectReferenceValue;
                            string seqName = targetSeq.name;
                            string buttonText = $" -> SEQ: <color=orange><b>{seqName}</b></color>";
                            Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(buttonText), _clickableLinkStyle, GUILayout.ExpandWidth(false));
                            if (GUI.Button(buttonRect, new GUIContent(buttonText), _clickableLinkStyle))
                            {
                                EditorGUIUtility.PingObject(targetSeq);
                                Selection.activeObject = targetSeq;
                            }
                        }
                        else if (!string.IsNullOrEmpty(nextNodeID_choiceProp.stringValue))
                        {
                            string targetNodeIdString = nextNodeID_choiceProp.stringValue;
                            Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent($" -> NODE: <color=cyan><b>{targetNodeIdString}</b></color>"), _clickableLinkStyle, GUILayout.ExpandWidth(false));
                            if (GUI.Button(buttonRect, new GUIContent($" -> NODE: <color=cyan><b>{targetNodeIdString}</b></color>"), _clickableLinkStyle))
                            {
                                FocusOnNode(targetNodeIdString, i);
                            }
                        }
                        else
                        {
                            GUILayout.Label(" (Ends Path)", _richMiniLabelStyle, GUILayout.ExpandWidth(false));
                        }
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.LabelField($"Node Completion Rewards ({nodeRewardsProp.arraySize})", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(nodeRewardsProp, true);

                    // Show node-level sequence transition
                    SerializedProperty nextSeqSOProp = nodeElementProp.FindPropertyRelative("nextSequenceSO");
                    if (nextSeqSOProp != null && nextSeqSOProp.objectReferenceValue != null)
                    {
                        CGDialogueSequenceSO targetSeq = (CGDialogueSequenceSO)nextSeqSOProp.objectReferenceValue;
                        string buttonText = $" → SEQ: <color=orange><b>{targetSeq.name}</b></color>";
                        Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(buttonText), _clickableLinkStyle, GUILayout.ExpandWidth(false));
                        if (GUI.Button(buttonRect, new GUIContent(buttonText), _clickableLinkStyle))
                        {
                            EditorGUIUtility.PingObject(targetSeq);
                            Selection.activeObject = targetSeq;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Separator();
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void FocusOnNode(string targetNodeID, int originatingNodeIndex)
    {
        if (string.IsNullOrEmpty(targetNodeID)) return;

        bool foundAndOpened = false;
        int targetNodeVisualIndex = -1;

        for (int i = 0; i < _nodeListProp.arraySize; i++)
        {
            SerializedProperty nodeElementProp = _nodeListProp.GetArrayElementAtIndex(i);
            SerializedProperty nodeIDProp = nodeElementProp.FindPropertyRelative("nodeID");
            if (nodeIDProp.stringValue == targetNodeID)
            {
                string uniqueFoldoutKey = $"{_sequenceSO.GetInstanceID()}_{nodeIDProp.stringValue}_{i}";
                _nodeFoldouts[uniqueFoldoutKey] = true;
                foundAndOpened = true;
                targetNodeVisualIndex = i;

                float estimatedHeaderHeight = EditorGUIUtility.singleLineHeight * 3;
                float estimatedEntryHeight = EditorGUIUtility.singleLineHeight * 1.5f;
                _nodeListScrollPosition.y = (estimatedEntryHeight * targetNodeVisualIndex) - estimatedHeaderHeight;
                _nodeListScrollPosition.y = Mathf.Max(0, _nodeListScrollPosition.y);
                Repaint();
                break;
            }
        }
        if (!foundAndOpened)
        {
            Debug.LogWarning($"[CGDialogueSequenceSOEditor] Node ID '{targetNodeID}' not found in the current sequence ('{_sequenceSO.name}') to focus/expand.");
        }
    }

    private bool IsNodeReferencedByChoice(string nodeIDToCheck, CGDialogueSequenceSO currentSequence)
    {
        if (string.IsNullOrEmpty(nodeIDToCheck) || currentSequence == null || currentSequence.nodeList == null) return false;

        foreach (CGDialogueNodeData sourceNode in currentSequence.nodeList)
        {
            if (sourceNode == null) continue;

            // Check choices
            if (sourceNode.choices != null)
            {
                foreach (CGDialogueChoiceData choice in sourceNode.choices)
                {
                    if (choice == null) continue;
                    if (choice.nextSequenceSO == null && !string.IsNullOrEmpty(choice.nextNodeID) && choice.nextNodeID == nodeIDToCheck)
                        return true;
#if UNITY_EDITOR
                    if (string.IsNullOrEmpty(choice._editor_nextSequenceID_temp) && !string.IsNullOrEmpty(choice._editor_nextNodeID_temp) && choice._editor_nextNodeID_temp == nodeIDToCheck)
                        return true;
#endif
                }
            }
        }
        return false;
    }

    private void DisplayRuntimeCsvPreview(string localizationKey)
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            EditorGUILayout.LabelField("<i>(No localization key)</i>", _richMiniLabelStyle);
            return;
        }

        if (RuntimeCsvLocalizationEditorUtility.TryGetText(
                localizationKey,
                LocalizationDomains.Dialogues,
                _selectedLocaleCode,
                out string text,
                out string usedLocale))
        {
            string localeLabel = usedLocale == _selectedLocaleCode ? usedLocale : $"{usedLocale} fallback";
            EditorGUILayout.LabelField($"<b>Key:</b> {localizationKey}", _richMiniLabelStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"<b>{localeLabel}:</b>", GUILayout.Width(90));
            EditorGUILayout.SelectableLabel(
                text,
                EditorStyles.wordWrappedLabel,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Missing runtime CSV key '{localizationKey}' in {_selectedLocaleCode} and en-US.",
                MessageType.Warning);
        }
    }

    // Navigation helper methods
    private void ExpandAllNodes()
    {
        for (int i = 0; i < _nodeListProp.arraySize; i++)
        {
            SerializedProperty nodeElementProp = _nodeListProp.GetArrayElementAtIndex(i);
            SerializedProperty nodeIDProp = nodeElementProp.FindPropertyRelative("nodeID");
            string nodeIDVal = nodeIDProp.stringValue;
            string uniqueFoldoutKey = $"{_sequenceSO.GetInstanceID()}_{nodeIDVal}_{i}";
            _nodeFoldouts[uniqueFoldoutKey] = true;
        }
        Repaint();
    }

    private void CollapseAllNodes()
    {
        _nodeFoldouts.Clear();
        Repaint();
    }

    private bool NodeContainsSearchText(SerializedProperty nodeElementProp, string searchText)
    {
        if (string.IsNullOrEmpty(searchText)) return true;

        // Search in dialogue lines (only check first few lines for performance)
        SerializedProperty linesProp = nodeElementProp.FindPropertyRelative("lines");
        int linesToCheck = Mathf.Min(linesProp.arraySize, 5);

        for (int i = 0; i < linesToCheck; i++)
        {
            SerializedProperty lineElementProp = linesProp.GetArrayElementAtIndex(i);
            SerializedProperty localizationKeyProp = lineElementProp.FindPropertyRelative("localizationKey");
            if (localizationKeyProp != null && localizationKeyProp.stringValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        // Search in choice texts (only check first few choices for performance)
        SerializedProperty choicesProp = nodeElementProp.FindPropertyRelative("choices");
        int choicesToCheck = Mathf.Min(choicesProp.arraySize, 5);

        for (int i = 0; i < choicesToCheck; i++)
        {
            SerializedProperty choiceElementProp = choicesProp.GetArrayElementAtIndex(i);
            SerializedProperty localizationKeyProp = choiceElementProp.FindPropertyRelative("localizationKey");
            if (localizationKeyProp != null && localizationKeyProp.stringValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            SerializedProperty choiceTextProp = choiceElementProp.FindPropertyRelative("choiceText");
            if (choiceTextProp != null && choiceTextProp.stringValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void BuildIncomingReferencesCache()
    {
        _incomingReferencesCache = new Dictionary<string, List<string>>();

        for (int i = 0; i < _sequenceSO.nodeList.Count; i++)
        {
            var sourceNode = _sequenceSO.nodeList[i];
            if (sourceNode == null) continue;

            // Track choice-level node references
            if (sourceNode.choices != null)
            {
                foreach (var choice in sourceNode.choices)
                {
                    if (choice == null || string.IsNullOrEmpty(choice.nextNodeID) || choice.nextSequenceSO != null) continue;
                    string targetNodeID = choice.nextNodeID;
                    if (!_incomingReferencesCache.ContainsKey(targetNodeID))
                        _incomingReferencesCache[targetNodeID] = new List<string>();
                    if (!_incomingReferencesCache[targetNodeID].Contains(sourceNode.nodeID))
                        _incomingReferencesCache[targetNodeID].Add(sourceNode.nodeID);
                }
            }
        }
    }
}
