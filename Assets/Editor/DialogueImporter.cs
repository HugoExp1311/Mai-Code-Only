using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Base.Dialogues;
using Base.CG; // NEW: Added for CG dialogue support
using System;
using Base.Logger;
using System.Text;
using System.Globalization;
using System.Linq;
using TMPro;
using System.Text.RegularExpressions;

// Unified DialogueImporter - Handles both regular dialogue and CG dialogue - Build 2025-01-25
// Auto-detects dialogue type based on CSV structure

public class DialogueImporter : EditorWindow
{
    private TextAsset csvFile;

    // Regular dialogue: 20 base columns (added FontStyle + IsMonologue)
    // CG dialogue: 16 base columns (added FontStyle + IsMonologue, no Animation/AnimFloat columns)
    private const int EXPECTED_COLUMNS_REGULAR_LEGACY = 20;
    private const int EXPECTED_COLUMNS_REGULAR_MULTILANG = 22; // 20 base + 2 languages
    private const int EXPECTED_COLUMNS_CG_LEGACY = 16;
    private const int EXPECTED_COLUMNS_CG_MULTILANG = 18; // 16 base + 2 languages
    private const string DialogueFallbackCsvPath = "Assets/Resources/Localization/Dialogues/en-US.csv";

    private bool isMultiLanguageCSV = false;
    private bool isCGDialogue = false; // Auto-detected based on column count
    private HashSet<string> fallbackDialogueLocalizationKeys;

    // New list to hold detected languages from the CSV header
    private List<string> detectedLanguages = new List<string>();

    [MenuItem("Tools/Dialogue Importer")]
    public static void ShowWindow()
    {
        GetWindow<DialogueImporter>("Dialogue Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Unified Dialogue Importer", EditorStyles.boldLabel);
        GUILayout.Label("Auto-detects Regular Dialogue or CG Dialogue from CSV", EditorStyles.miniLabel);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Structure-only importer. Display text is resolved at runtime from Assets/Resources/Localization CSV files.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Import buttons
        if (GUILayout.Button("Import Dialogue", GUILayout.Height(30)))
        {
            if (csvFile != null)
            {
                Debug.Log($"[DialogueImporter] Starting import process...");
                Debug.Log($"[DialogueImporter] CSV File: {csvFile.name}");

                FileLogger.Initialize();
                FileLogger.Log($"[DialogueImporter] Import started for file: {csvFile.name}");

                try
                {
                    string path = AssetDatabase.GetAssetPath(csvFile);
                    Debug.Log($"[DialogueImporter] CSV Path: {path}");
                    FileLogger.Log($"[DialogueImporter] CSV Path: {path}");

                    ImportDialogueFromCSV(path);
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"[ImportDialogueFromCSV] FATAL ERROR: {ex.ToString()}");
                    Debug.LogError($"[ImportDialogueFromCSV] FATAL ERROR: {ex.ToString()}");
                }
                finally
                {
                    FileLogger.Close();
                    Debug.Log("Dialogue Import process finished. Check Log for details.");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a CSV file.", "OK");
            }
        }

        // Help section
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("CSV Format (Auto-Detected):", EditorStyles.boldLabel);
        GUILayout.Label("• Regular Dialogue: 20 base columns (with Animation/AnimFloat/FontStyle/IsMonologue)");
        GUILayout.Label("• CG Dialogue: 16 base columns (with CGSpritePath/FontStyle/IsMonologue instead)");
        GUILayout.Label("• Text columns are externalized; use Localization/Dialogues/{locale}.csv");
        EditorGUILayout.EndVertical();
    }

    private void ImportDialogueFromCSV(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            FileLogger.Log($"File not found: {filePath}");
            return;
        }

        string[] csvLines = File.ReadAllLines(filePath);
        fallbackDialogueLocalizationKeys = null;

        if (csvLines.Length <= 1)
        {
            Debug.LogError("CSV is empty or has only a header.");
            FileLogger.Log("CSV is empty or has only a header.");
            return;
        }

        Dictionary<string, ScriptableObject> allImportedSequences = new Dictionary<string, ScriptableObject>();

        // Detect CSV format by examining header
        if (csvLines.Length > 0)
        {
            string headerLine = csvLines[0];
            string[] headerCells = ParseCsvLine(headerLine, -1); // No limit for header parsing

            Debug.Log($"CSV Header: {headerCells.Length} columns detected");

            // Reset detected languages
            detectedLanguages.Clear();

            // Auto-detect dialogue type based on column count
            if (headerCells.Length == EXPECTED_COLUMNS_CG_LEGACY || headerCells.Length == EXPECTED_COLUMNS_CG_MULTILANG)
            {
                isCGDialogue = true;
                Debug.Log("✓ Detected CG Dialogue format");
                FileLogger.Log("Detected CG Dialogue format");
            }
            else if (headerCells.Length == EXPECTED_COLUMNS_REGULAR_LEGACY || headerCells.Length == EXPECTED_COLUMNS_REGULAR_MULTILANG)
            {
                isCGDialogue = false;
                Debug.Log("✓ Detected Regular Dialogue format");
                FileLogger.Log("Detected Regular Dialogue format");
            }
            else
            {
                Debug.LogError($"Unknown CSV format with {headerCells.Length} columns. Expected 16/18 (CG) or 20/22 (Regular)");
                FileLogger.Log($"ERROR: Unknown CSV format with {headerCells.Length} columns");
                return;
            }

            // Detect multi-language support
            int baseCols = isCGDialogue ? EXPECTED_COLUMNS_CG_LEGACY : EXPECTED_COLUMNS_REGULAR_LEGACY;
            if (headerCells.Length > baseCols)
            {
                isMultiLanguageCSV = true;

                // Languages are in columns after the base ones
                for (int i = baseCols; i < headerCells.Length; i++)
                {
                    string header = headerCells[i].Trim();
                    var match = Regex.Match(header, @"\(([^)]+)\)");
                    if (match.Success)
                    {
                        detectedLanguages.Add(match.Groups[1].Value);
                    }
                    else
                    {
                        detectedLanguages.Add(header); // Fallback to using the full header if no code is found
                    }
                }

                Debug.LogWarning($"Detected deprecated multi-language CSV columns ({string.Join(", ", detectedLanguages)}). Importer will use structure data only; move translations to runtime locale CSVs.");
                FileLogger.Log($"Detected deprecated multi-language CSV columns ({string.Join(", ", detectedLanguages)}). Structure import only.");
            }
            else
            {
                isMultiLanguageCSV = false;
                Debug.Log($"Detected structure-only CSV format ({headerCells.Length} columns)");
                FileLogger.Log($"Detected structure-only CSV format ({headerCells.Length} columns)");
            }
        }

        Debug.Log($"Import settings: isMultiLanguageCSV={isMultiLanguageCSV}, textSource=Runtime CSV localization");

        FileLogger.Log("--- STARTING PASS 1: Reading CSV and Populating SO Data ---");

        for (int i = 1; i < csvLines.Length; i++)
        {
            string rawCsvLine = csvLines[i];

            if (string.IsNullOrWhiteSpace(rawCsvLine))
                continue;

            Debug.Log($"--- Processing CSV Row {i + 1} ---");
            FileLogger.Log($"--- Processing CSV Row {i + 1} ---");

            // Use detected dialogue type to determine expected columns
            int expectedColumns;
            if (isCGDialogue)
            {
                expectedColumns = isMultiLanguageCSV ? EXPECTED_COLUMNS_CG_MULTILANG : EXPECTED_COLUMNS_CG_LEGACY;
            }
            else
            {
                expectedColumns = isMultiLanguageCSV ? EXPECTED_COLUMNS_REGULAR_MULTILANG : EXPECTED_COLUMNS_REGULAR_LEGACY;
            }

            string[] cells = ParseCsvLine(rawCsvLine, expectedColumns);

            if (cells.Length != expectedColumns)
            {
                Debug.LogError($"Row {i + 1}: Incorrect column count. Expected {expectedColumns}, got {cells.Length}. SKIPPING.");
                FileLogger.Log($"Row {i + 1}: Incorrect column count. Expected {expectedColumns}, got {cells.Length}. SKIPPING. Line: {rawCsvLine}");
                continue;
            }

            string csv_sequenceID = GetCellContent(cells, 0);
            string csv_nodeType = GetCellContent(cells, 1).ToUpper();

            if (string.IsNullOrEmpty(csv_sequenceID))
            {
                Debug.Log($"Row {i + 1}: Empty SequenceID. Skipping.");
                FileLogger.Log($"Row {i + 1}: Empty SequenceID. Skipping.");
                continue;
            }

            ScriptableObject currentSequenceSO;

            if (!allImportedSequences.TryGetValue(csv_sequenceID, out currentSequenceSO))
            {
                currentSequenceSO = CreateOrGetSequenceSO(csv_sequenceID);
                allImportedSequences[csv_sequenceID] = currentSequenceSO;

                // Call ClearEditorTempCache based on type
                if (isCGDialogue && currentSequenceSO is CGDialogueSequenceSO cgSeq)
                {
                    cgSeq.ClearEditorTempCache();
                }
                else if (!isCGDialogue && currentSequenceSO is DialogueSequenceSO regSeq)
                {
                    regSeq.ClearEditorTempCache();
                }
            }

            switch (csv_nodeType)
            {
                case "SEQUENCE_START":
                    ProcessSequenceStart(cells, currentSequenceSO);
                    break;
                case "NODE":
                    ProcessNode(cells, currentSequenceSO, i + 1);
                    break;
                case "CHOICE":
                    ProcessChoice(cells, currentSequenceSO, i + 1);
                    break;
                case "SEQUENCE_END":
                    ProcessSequenceEnd(cells, currentSequenceSO, i + 1);
                    break;
                default:
                    Debug.Log($"Row {i + 1}: Unknown NodeType '{csv_nodeType}' for Sequence '{csv_sequenceID}'. Skipping.");
                    FileLogger.Log($"Row {i + 1}: Unknown NodeType '{csv_nodeType}' for Sequence '{csv_sequenceID}'. Skipping.");
                    break;
            }
        }

        Debug.Log("--- STARTING PASS 2: Linking Choices and Random Outcomes ---");
        FileLogger.Log("--- STARTING PASS 2: Linking Choices and Random Outcomes ---");
        LinkCrossSequenceData(allImportedSequences, csvLines);

        // CRITICAL FIX: Mark ALL sequences as dirty AND force immediate save
        // This is especially important for CG sequences that don't have nextSequenceSO links
        foreach (var kvp in allImportedSequences)
        {
            EditorUtility.SetDirty(kvp.Value);
            // Force Unity to write this specific asset to disk immediately
            AssetDatabase.SaveAssetIfDirty(kvp.Value);
        }
        FileLogger.Log("Marked all sequences as dirty and saved to disk");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("--- Dialogue Import Complete ---");
        FileLogger.Log("--- Dialogue Import Complete ---");
    }

    private void ProcessSequenceStart(string[] cells, ScriptableObject sequenceSO)
    {
        string sequenceID = GetCellContent(cells, 0);
        string explicitStartingNodeID = GetCellContent(cells, isCGDialogue ? 15 : 16); // Shifted +2 for FontStyle + IsMonologue columns
        string randomGoodID = isCGDialogue ? string.Empty : GetCellContent(cells, 17); // Shifted +2 for FontStyle + IsMonologue columns
        string randomNeutralID = isCGDialogue ? string.Empty : GetCellContent(cells, 18); // Shifted +2 for FontStyle + IsMonologue columns
        string randomBadID = isCGDialogue ? string.Empty : GetCellContent(cells, 19); // Shifted +2 for FontStyle + IsMonologue columns

        // Cast to appropriate type and set properties
        if (isCGDialogue && sequenceSO is CGDialogueSequenceSO cgSeq)
        {
            cgSeq.sequenceID = sequenceID;
            if (!string.IsNullOrEmpty(explicitStartingNodeID))
                cgSeq.startingNodeID = explicitStartingNodeID;

            FileLogger.Log($"  SEQUENCE_START (CG): ID='{cgSeq.sequenceID}', StartNode='{cgSeq.startingNodeID}'");
        }
        else if (!isCGDialogue && sequenceSO is DialogueSequenceSO regSeq)
        {
            regSeq.sequenceID = sequenceID;
            if (!string.IsNullOrEmpty(explicitStartingNodeID))
                regSeq.startingNodeID = explicitStartingNodeID;

            if (!string.IsNullOrEmpty(randomGoodID) || !string.IsNullOrEmpty(randomNeutralID) || !string.IsNullOrEmpty(randomBadID))
                regSeq.type = SequenceType.RandomOutcome;
            else
                regSeq.type = SequenceType.Linear;

            FileLogger.Log($"  SEQUENCE_START (Regular): ID='{regSeq.sequenceID}', Type='{regSeq.type}', StartNode='{regSeq.startingNodeID}'");
        }
    }

    private void ProcessNode(string[] cells, ScriptableObject currentSequenceSO, int rowNum)
    {
        // Route to appropriate handler based on dialogue type
        if (isCGDialogue)
        {
            ProcessCGNode(cells, currentSequenceSO, rowNum);
        }
        else
        {
            ProcessRegularNode(cells, currentSequenceSO, rowNum);
        }
    }

    private void ProcessRegularNode(string[] cells, ScriptableObject currentSequenceSO, int rowNum)
    {
        // Cast to DialogueSequenceSO for regular dialogue processing
        if (!(currentSequenceSO is DialogueSequenceSO regSeq))
        {
            Debug.LogError($"Row {rowNum}: ProcessRegularNode called with non-DialogueSequenceSO object. Skipping.");
            FileLogger.Log($"Row {rowNum}: ProcessRegularNode called with non-DialogueSequenceSO object. Skipping.");
            return;
        }

        string csv_nodeID = GetCellContent(cells, 2);

        if (string.IsNullOrEmpty(csv_nodeID))
        {
            Debug.Log($"Row {rowNum}: NODE missing NodeID in Sequence '{regSeq.sequenceID}'. Skipping.");
            FileLogger.Log($"Row {rowNum}: NODE missing NodeID in Sequence '{regSeq.sequenceID}'. Skipping.");
            return;
        }

        DialogueNodeData nodeData = regSeq.GetOrCreateEditorNode(csv_nodeID);

        if (regSeq.type == SequenceType.Linear && string.IsNullOrEmpty(regSeq.startingNodeID))
        {
            regSeq.startingNodeID = csv_nodeID;
            Debug.Log($"    Set starting node for Linear Sequence '{regSeq.sequenceID}' to '{csv_nodeID}'.");
            FileLogger.Log($"    Set starting node for Linear Sequence '{regSeq.sequenceID}' to '{csv_nodeID}'.");
        }

        string csv_speakerStr = GetCellContent(cells, 5);
        string csv_lineText = GetCellContent(cells, 6);

        string localizationKey = $"{regSeq.sequenceID}.{csv_nodeID}";
        bool hasRuntimeTextSource = ShouldAssignLineLocalizationKey(localizationKey, csv_lineText);

        if (hasRuntimeTextSource || !string.IsNullOrEmpty(csv_speakerStr))
        {
            DialogueLine newLine = new DialogueLine();
            newLine.localizationKey = hasRuntimeTextSource ? localizationKey : string.Empty;

            // Handle speaker field with proper mapping and error handling
            string mappedSpeaker = MapSpeakerName(csv_speakerStr);
            if (!Enum.TryParse(mappedSpeaker, true, out newLine.speaker))
            {
                Debug.LogWarning($"    WARNING: Failed to parse speaker '{csv_speakerStr}' (mapped to '{mappedSpeaker}') for node '{csv_nodeID}', defaulting to Mai");
                FileLogger.Log($"    WARNING: Failed to parse speaker '{csv_speakerStr}' (mapped to '{mappedSpeaker}') for node '{csv_nodeID}', defaulting to Mai");
                newLine.speaker = Base.Character.Stats.RewardTarget.Mai;
            }

            Enum.TryParse(GetCellContent(cells, 7), true, out newLine.animationToPlay);
            // LoopAnim column removed - all animations now loop by default via Trigger + Bool pattern
            newLine.loopAnimation = true; // Always true - controlled by animator Bool parameter

            // Use culture-invariant parsing for float values to prevent locale issues
            string floatValueStr = GetCellContent(cells, 8).Trim('"'); // AnimFloat column
            if (!float.TryParse(floatValueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out newLine.animationFloatValue))
            {
                if (!string.IsNullOrEmpty(floatValueStr))
                {
                    Debug.LogWarning($"    WARNING: Failed to parse animationFloatValue '{floatValueStr}' for node '{csv_nodeID}', defaulting to 0.5");
                    FileLogger.Log($"    WARNING: Failed to parse animationFloatValue '{floatValueStr}' for node '{csv_nodeID}', defaulting to 0.5");
                }
                newLine.animationFloatValue = 0.5f;
            }

            // Parse FontStyle from column 9
            string fontStyleStr = GetCellContent(cells, 9);
            if (!string.IsNullOrEmpty(fontStyleStr))
            {
                if (Enum.TryParse(fontStyleStr, true, out FontStyles parsedStyle))
                {
                    newLine.fontStyle = parsedStyle;
                }
                else
                {
                    Debug.LogWarning($"    WARNING: Failed to parse FontStyle '{fontStyleStr}' for node '{csv_nodeID}', defaulting to Normal");
                    FileLogger.Log($"    WARNING: Failed to parse FontStyle '{fontStyleStr}' for node '{csv_nodeID}', defaulting to Normal");
                    newLine.fontStyle = FontStyles.Normal;
                }
            }

            // Parse IsMonologue from column 10
            string isMonologueStr = GetCellContent(cells, 10);
            if (!string.IsNullOrEmpty(isMonologueStr))
            {
                newLine.isMonologue = isMonologueStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            }

            nodeData.lines.Add(newLine);
            Debug.Log($"    NODE: ID='{csv_nodeID}', Speaker='{newLine.speaker}', LocalizationKey='{(string.IsNullOrEmpty(newLine.localizationKey) ? "<none>" : newLine.localizationKey)}', TextSource='{(hasRuntimeTextSource ? "Runtime CSV" : "None")}', InlineText='{DescribeInlineText(csv_lineText)}'");
            FileLogger.Log($"    NODE: ID='{csv_nodeID}', Speaker='{newLine.speaker}', LocalizationKey='{(string.IsNullOrEmpty(newLine.localizationKey) ? "<none>" : newLine.localizationKey)}', TextSource='{(hasRuntimeTextSource ? "Runtime CSV" : "None")}', InlineText='{DescribeInlineText(csv_lineText)}'");
        }
        else
        {
            Debug.Log($"    NODE: ID='{csv_nodeID}' (choices-only or empty line)");
            FileLogger.Log($"    NODE: ID='{csv_nodeID}' (choices-only or empty line)");
        }

        // Check for node-level rewards in columns 11-13 (RewardTarget, RewardStat, RewardAmount) - shifted +2 for FontStyle + IsMonologue columns
        string csv_rewardTargetStr = GetCellContent(cells, 11);
        string csv_rewardStatStr = GetCellContent(cells, 12);
        string csv_rewardAmountStr = GetCellContent(cells, 13);

        if (!string.IsNullOrEmpty(csv_rewardStatStr) && !string.IsNullOrEmpty(csv_rewardTargetStr))
        {
            DialogueReward nodeReward = new DialogueReward();

            // Map and parse reward target
            string mappedTarget = MapRewardTarget(csv_rewardTargetStr);
            bool targetParsed = Enum.TryParse(mappedTarget, true, out nodeReward.target);
            bool statParsed = Enum.TryParse(csv_rewardStatStr, true, out nodeReward.stat);
            int.TryParse(csv_rewardAmountStr, out nodeReward.amount);

            if (targetParsed && statParsed)
            {
                nodeData.rewardsOnNodeCompletion.Add(nodeReward);
                Debug.Log($"    Added Node Reward: {nodeReward.target}/{nodeReward.stat} ({nodeReward.amount})");
                FileLogger.Log($"    Added Node Reward: {nodeReward.target}/{nodeReward.stat} ({nodeReward.amount})");
            }
            else
            {
                string failReason = !targetParsed ? $"TargetParseFail('{csv_rewardTargetStr}'->'{{mappedTarget}}') " : "";
                failReason += !statParsed ? $"StatParseFail('{csv_rewardStatStr}') " : "";
                Debug.LogError($"    Node Reward Parse FAILED: {failReason.Trim()}");
                FileLogger.Log($"    Node Reward Parse FAILED: {failReason.Trim()}");
            }
        }
    }

    private void ProcessCGNode(string[] cells, ScriptableObject currentSequenceSO, int rowNum)
    {
        // Cast to CGDialogueSequenceSO for CG dialogue processing
        if (!(currentSequenceSO is CGDialogueSequenceSO cgSeq))
        {
            Debug.LogError($"Row {rowNum}: ProcessCGNode called with non-CGDialogueSequenceSO object. Skipping.");
            FileLogger.Log($"Row {rowNum}: ProcessCGNode called with non-CGDialogueSequenceSO object. Skipping.");
            return;
        }

        // CG dialogue processing - no Animation/AnimFloat columns
        string csv_nodeID = GetCellContent(cells, 2);

        if (string.IsNullOrEmpty(csv_nodeID))
        {
            Debug.Log($"Row {rowNum}: CG NODE missing NodeID in Sequence '{cgSeq.sequenceID}'. Skipping.");
            FileLogger.Log($"Row {rowNum}: CG NODE missing NodeID in Sequence '{cgSeq.sequenceID}'. Skipping.");
            return;
        }

        CGDialogueNodeData nodeData = cgSeq.GetOrCreateEditorNode(csv_nodeID);

        if (string.IsNullOrEmpty(cgSeq.startingNodeID))
        {
            cgSeq.startingNodeID = csv_nodeID;
            Debug.Log($"    Set starting node for CG Sequence '{cgSeq.sequenceID}' to '{csv_nodeID}'.");
            FileLogger.Log($"    Set starting node for CG Sequence '{cgSeq.sequenceID}' to '{csv_nodeID}'.");
        }

        string csv_speakerStr = GetCellContent(cells, 5);
        string csv_lineText = GetCellContent(cells, 6);
        string csv_cgSpritePath = GetCellContent(cells, 7); // CG sprite path instead of Animation

        string localizationKey = $"{cgSeq.sequenceID}.{csv_nodeID}";
        bool hasRuntimeTextSource = ShouldAssignLineLocalizationKey(localizationKey, csv_lineText);

        if (hasRuntimeTextSource || !string.IsNullOrEmpty(csv_speakerStr))
        {
            CGDialogueLine newLine = new CGDialogueLine();
            newLine.localizationKey = hasRuntimeTextSource ? localizationKey : string.Empty;

            // Handle speaker field with proper mapping and error handling
            string mappedSpeaker = MapSpeakerName(csv_speakerStr);
            if (!Enum.TryParse(mappedSpeaker, true, out newLine.speaker))
            {
                Debug.LogWarning($"    WARNING: Failed to parse speaker '{csv_speakerStr}' (mapped to '{mappedSpeaker}') for node '{csv_nodeID}', defaulting to Mai");
                FileLogger.Log($"    WARNING: Failed to parse speaker '{csv_speakerStr}' (mapped to '{mappedSpeaker}') for node '{csv_nodeID}', defaulting to Mai");
                newLine.speaker = Base.Character.Stats.RewardTarget.Mai;
            }

            // Legacy text field removed - all dialogue must use localization

            // Load CG sprite from path
            if (!string.IsNullOrEmpty(csv_cgSpritePath))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(csv_cgSpritePath);
                if (sprite != null)
                {
                    newLine.cgSprite = sprite;
                    Debug.Log($"    Loaded CG sprite: {csv_cgSpritePath}");
                    FileLogger.Log($"    Loaded CG sprite: {csv_cgSpritePath}");
                }
                else
                {
                    Debug.LogWarning($"    WARNING: CG sprite not found at path: {csv_cgSpritePath}");
                    FileLogger.Log($"    WARNING: CG sprite not found at path: {csv_cgSpritePath}");
                }
            }

            // Parse FontStyle from column 8
            string fontStyleStr = GetCellContent(cells, 8);
            if (!string.IsNullOrEmpty(fontStyleStr))
            {
                if (Enum.TryParse(fontStyleStr, true, out FontStyles parsedStyle))
                {
                    newLine.fontStyle = parsedStyle;
                }
                else
                {
                    Debug.LogWarning($"    WARNING: Failed to parse FontStyle '{fontStyleStr}' for CG node '{csv_nodeID}', defaulting to Normal");
                    FileLogger.Log($"    WARNING: Failed to parse FontStyle '{fontStyleStr}' for CG node '{csv_nodeID}', defaulting to Normal");
                    newLine.fontStyle = FontStyles.Normal;
                }
            }

            // Parse IsMonologue from column 9
            string isMonologueStr = GetCellContent(cells, 9);
            if (!string.IsNullOrEmpty(isMonologueStr))
            {
                newLine.isMonologue = isMonologueStr.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            }

            // CG dialogue lines don't have animation fields
            nodeData.lines.Add(newLine);
            Debug.Log($"    CG NODE: ID='{csv_nodeID}', Speaker='{newLine.speaker}', LocalizationKey='{(string.IsNullOrEmpty(newLine.localizationKey) ? "<none>" : newLine.localizationKey)}', TextSource='{(hasRuntimeTextSource ? "Runtime CSV" : "None")}', InlineText='{DescribeInlineText(csv_lineText)}', Sprite='{(newLine.cgSprite != null ? newLine.cgSprite.name : "NONE")}'");
            FileLogger.Log($"    CG NODE: ID='{csv_nodeID}', Speaker='{newLine.speaker}', LocalizationKey='{(string.IsNullOrEmpty(newLine.localizationKey) ? "<none>" : newLine.localizationKey)}', TextSource='{(hasRuntimeTextSource ? "Runtime CSV" : "None")}', InlineText='{DescribeInlineText(csv_lineText)}', Sprite='{(newLine.cgSprite != null ? newLine.cgSprite.name : "NONE")}'");
        }
        else
        {
            Debug.Log($"    CG NODE: ID='{csv_nodeID}' (choices-only or empty line)");
            FileLogger.Log($"    CG NODE: ID='{csv_nodeID}' (choices-only or empty line)");
        }

        // Read NextNodeID from column 13 (explicit intra-sequence successor override) - shifted +2 for FontStyle + IsMonologue
        string csv_nextNodeID = GetCellContent(cells, 13);
        if (!string.IsNullOrEmpty(csv_nextNodeID))
        {
            nodeData.nextNodeID = csv_nextNodeID;
            Debug.Log($"    CG NODE: ID='{csv_nodeID}', NextNodeID='{csv_nextNodeID}' (explicit override)");
            FileLogger.Log($"    CG NODE: ID='{csv_nodeID}', NextNodeID='{csv_nextNodeID}' (explicit override)");
        }

        // Read NextSequenceID from column 14 (node-level sequence transition override) - shifted +2 for FontStyle + IsMonologue
        string csv_nextSequenceID_node = GetCellContent(cells, 14);
        if (!string.IsNullOrEmpty(csv_nextSequenceID_node))
        {
            nodeData._editor_nextSequenceID_temp = csv_nextSequenceID_node;
            Debug.Log($"    CG NODE: ID='{csv_nodeID}', NextSequenceID='{csv_nextSequenceID_node}' (node-level transition)");
            FileLogger.Log($"    CG NODE: ID='{csv_nodeID}', NextSequenceID='{csv_nextSequenceID_node}' (node-level transition)");
        }

        // Check for node-level rewards in columns 10-12 (RewardTarget, RewardStat, RewardAmount) for CG dialogue - shifted +2 for FontStyle + IsMonologue
        string csv_rewardTargetStr = GetCellContent(cells, 10);
        string csv_rewardStatStr = GetCellContent(cells, 11);
        string csv_rewardAmountStr = GetCellContent(cells, 12);

        if (!string.IsNullOrEmpty(csv_rewardStatStr) && !string.IsNullOrEmpty(csv_rewardTargetStr))
        {
            DialogueReward nodeReward = new DialogueReward();

            // Map and parse reward target
            string mappedTarget = MapRewardTarget(csv_rewardTargetStr);
            bool targetParsed = Enum.TryParse(mappedTarget, true, out nodeReward.target);
            bool statParsed = Enum.TryParse(csv_rewardStatStr, true, out nodeReward.stat);
            int.TryParse(csv_rewardAmountStr, out nodeReward.amount);

            if (targetParsed && statParsed)
            {
                nodeData.rewardsOnNodeCompletion.Add(nodeReward);
                Debug.Log($"    Added CG Node Reward: {nodeReward.target}/{nodeReward.stat} ({nodeReward.amount})");
                FileLogger.Log($"    Added CG Node Reward: {nodeReward.target}/{nodeReward.stat} ({nodeReward.amount})");
            }
            else
            {
                string failReason = !targetParsed ? $"TargetParseFail('{csv_rewardTargetStr}'->'{{mappedTarget}}') " : "";
                failReason += !statParsed ? $"StatParseFail('{csv_rewardStatStr}') " : "";
                Debug.LogError($"    CG Node Reward Parse FAILED: {failReason.Trim()}");
                FileLogger.Log($"    CG Node Reward Parse FAILED: {failReason.Trim()}");
            }
        }
    }

    private void ProcessChoice(string[] cells, ScriptableObject currentSequenceSO, int rowNum)
    {
        string csv_parentNodeID = GetCellContent(cells, 3);
        string csv_choiceText = GetCellContent(cells, 4);

        if (string.IsNullOrEmpty(csv_parentNodeID))
        {
            // Get sequence ID for logging
            string seqID = "UNKNOWN";
            if (currentSequenceSO is DialogueSequenceSO regSeq)
                seqID = regSeq.sequenceID;
            else if (currentSequenceSO is CGDialogueSequenceSO cgSeq)
                seqID = cgSeq.sequenceID;

            Debug.Log($"Row {rowNum}: CHOICE missing ParentNodeID in Sequence '{seqID}'. Skipping.");
            FileLogger.Log($"Row {rowNum}: CHOICE missing ParentNodeID in Sequence '{seqID}'. Skipping.");
            return;
        }

        // Get parent node based on sequence type
        if (isCGDialogue && currentSequenceSO is CGDialogueSequenceSO cgSeq2)
        {
            CGDialogueNodeData parentNode = cgSeq2.GetOrCreateEditorNode(csv_parentNodeID);
            ProcessCGChoice(cells, cgSeq2, parentNode, csv_choiceText, rowNum);
        }
        else if (!isCGDialogue && currentSequenceSO is DialogueSequenceSO regSeq2)
        {
            DialogueNodeData parentNode = regSeq2.GetOrCreateEditorNode(csv_parentNodeID);
            ProcessRegularChoice(cells, regSeq2, parentNode, csv_choiceText, rowNum);
        }
    }

    private void ProcessRegularChoice(string[] cells, DialogueSequenceSO currentSequenceSO, DialogueNodeData parentNode, string csv_choiceText, int rowNum)
    {
        int choiceNumber = parentNode.choices.Count + 1;
        DialogueChoiceData newChoice = new DialogueChoiceData
        {
            localizationKey = $"{currentSequenceSO.sequenceID}.{parentNode.nodeID}.choice.{choiceNumber}",
            choiceText = csv_choiceText,
            _editor_nextNodeID_temp = GetCellContent(cells, 14), // Shifted +2 for FontStyle + IsMonologue columns
            _editor_nextSequenceID_temp = GetCellContent(cells, 15), // Shifted +2 for FontStyle + IsMonologue columns
            _editor_startingNodeInNextSequenceID_temp = GetCellContent(cells, 16) // Shifted +2 for FontStyle + IsMonologue columns
        };
        parentNode.choices.Add(newChoice);

        Debug.Log($"      CHOICE on Node '{parentNode.nodeID}': LocalizationKey='{newChoice.localizationKey}', TextSource='Runtime CSV', InlineText='{DescribeInlineText(csv_choiceText)}', TargetNode='{newChoice._editor_nextNodeID_temp}', TargetSeq='{newChoice._editor_nextSequenceID_temp}'");
        FileLogger.Log($"      CHOICE on Node '{parentNode.nodeID}': LocalizationKey='{newChoice.localizationKey}', TextSource='Runtime CSV', InlineText='{DescribeInlineText(csv_choiceText)}', TargetNode='{newChoice._editor_nextNodeID_temp}', TargetSeq='{newChoice._editor_nextSequenceID_temp}', StartInNext='{newChoice._editor_startingNodeInNextSequenceID_temp}'");
    }

    private void ProcessCGChoice(string[] cells, CGDialogueSequenceSO currentSequenceSO, CGDialogueNodeData parentNode, string csv_choiceText, int rowNum)
    {
        int choiceNumber = parentNode.choices.Count + 1;
        CGDialogueChoiceData newChoice = new CGDialogueChoiceData
        {
            localizationKey = $"{currentSequenceSO.sequenceID}.{parentNode.nodeID}.choice.{choiceNumber}",
            choiceText = csv_choiceText,
            _editor_nextNodeID_temp = GetCellContent(cells, 13), // CG dialogue: shifted +2 for FontStyle + IsMonologue
            _editor_nextSequenceID_temp = GetCellContent(cells, 14), // CG dialogue: shifted +2 for FontStyle + IsMonologue
            _editor_startingNodeInNextSequenceID_temp = GetCellContent(cells, 15) // CG dialogue: shifted +2 for FontStyle + IsMonologue
        };
        parentNode.choices.Add(newChoice);

        Debug.Log($"      CG CHOICE on Node '{parentNode.nodeID}': LocalizationKey='{newChoice.localizationKey}', TextSource='Runtime CSV', InlineText='{DescribeInlineText(csv_choiceText)}', TargetNode='{newChoice._editor_nextNodeID_temp}', TargetSeq='{newChoice._editor_nextSequenceID_temp}'");
        FileLogger.Log($"      CG CHOICE on Node '{parentNode.nodeID}': LocalizationKey='{newChoice.localizationKey}', TextSource='Runtime CSV', InlineText='{DescribeInlineText(csv_choiceText)}', TargetNode='{newChoice._editor_nextNodeID_temp}', TargetSeq='{newChoice._editor_nextSequenceID_temp}', StartInNext='{newChoice._editor_startingNodeInNextSequenceID_temp}'");
    }

    private void ProcessSequenceEnd(string[] cells, ScriptableObject currentSequenceSO, int rowNum)
    {
        string csv_reward_node_id = GetCellContent(cells, 2);

        // Mark the specified node as an end-node (branch terminal)
        if (!string.IsNullOrEmpty(csv_reward_node_id) && isCGDialogue && currentSequenceSO is CGDialogueSequenceSO cgSeqForEnd)
        {
            var existingNode = cgSeqForEnd.nodeList.Find(n => n.nodeID == csv_reward_node_id);
            if (existingNode != null)
            {
                existingNode.isEndNode = true;
                FileLogger.Log($"  Marked node '{csv_reward_node_id}' as end-node in CG sequence '{cgSeqForEnd.sequenceID}'");
            }
        }

        // CRITICAL FIX: Column indices differ between CG and Regular dialogue
        // CG dialogue: RewardTarget=10, RewardStat=11, RewardAmount=12, NextSequenceID=14 (shifted +2 for FontStyle + IsMonologue)
        // Regular dialogue: RewardTarget=11, RewardStat=12, RewardAmount=13, NextSequenceID=14 (shifted +2 for FontStyle + IsMonologue)
        int rewardTargetCol = isCGDialogue ? 10 : 11;
        int rewardStatCol = isCGDialogue ? 11 : 12;
        int rewardAmountCol = isCGDialogue ? 12 : 13;

        string csv_rewardTargetStr = GetCellContent(cells, rewardTargetCol);
        string csv_rewardStatStr = GetCellContent(cells, rewardStatCol);
        int.TryParse(GetCellContent(cells, rewardAmountCol), out int csv_rewardAmount);

        // NEW: Support for auto-continue to next sequence (column 14 for both dialogue types - shifted +2 for FontStyle + IsMonologue)
        string csv_nextSequenceID = GetCellContent(cells, 14);

        // Get sequence ID for logging
        string seqID = "UNKNOWN";
        if (currentSequenceSO is DialogueSequenceSO regSeq)
            seqID = regSeq.sequenceID;
        else if (currentSequenceSO is CGDialogueSequenceSO cgSeq)
            seqID = cgSeq.sequenceID;

        FileLogger.Log($"  SEQUENCE_END: Seq='{seqID}', Reward applies to Node='{csv_reward_node_id}' (if specified), NextSeq='{csv_nextSequenceID}' (if specified)");
        FileLogger.Log($"    RewardParams: Target='{csv_rewardTargetStr}', Stat='{csv_rewardStatStr}', Amount='{csv_rewardAmount}'");

        if (string.IsNullOrEmpty(csv_rewardStatStr))
        {
            FileLogger.Log("      No RewardStat specified. No reward added.");
            return;
        }

        DialogueReward reward = new DialogueReward();

        // Map and parse reward target
        string mappedTarget = MapRewardTarget(csv_rewardTargetStr);
        bool targetParsed = Enum.TryParse(mappedTarget, true, out reward.target);
        bool statParsed = Enum.TryParse(csv_rewardStatStr, true, out reward.stat);
        reward.amount = csv_rewardAmount;

        if (!targetParsed || !statParsed)
        {
            string failReason = !targetParsed ? $"TargetEnum_Fail ('{csv_rewardTargetStr}') " : "";
            failReason += !statParsed ? $"StatEnum_Fail ('{csv_rewardStatStr}')" : "";
            FileLogger.Log($"      Reward Parse FAILED: {failReason}. Reward NOT added.");
            return;
        }

        // Handle rewards based on sequence type
        if (isCGDialogue && currentSequenceSO is CGDialogueSequenceSO cgSeq2)
        {
            if (string.IsNullOrEmpty(csv_reward_node_id))
            {
                cgSeq2.rewardsOnCompletion.Add(reward);
                FileLogger.Log($"      Added GENERAL Reward to CG Sequence '{cgSeq2.sequenceID}': {reward.target}/{reward.stat} ({reward.amount}). Seq rewards: {cgSeq2.rewardsOnCompletion.Count}");
            }
            else
            {
                CGDialogueNodeData targetNodeForReward = cgSeq2.GetOrCreateEditorNode(csv_reward_node_id);
                targetNodeForReward.rewardsOnNodeCompletion.Add(reward);
                FileLogger.Log($"      Added Reward to CG Node '{targetNodeForReward.nodeID}': {reward.target}/{reward.stat} ({reward.amount}). Node rewards: {targetNodeForReward.rewardsOnNodeCompletion.Count}");
            }
        }
        else if (!isCGDialogue && currentSequenceSO is DialogueSequenceSO regSeq2)
        {
            if (string.IsNullOrEmpty(csv_reward_node_id))
            {
                regSeq2.rewardsOnCompletion.Add(reward);
                FileLogger.Log($"      Added GENERAL Reward to Sequence '{regSeq2.sequenceID}': {reward.target}/{reward.stat} ({reward.amount}). Seq rewards: {regSeq2.rewardsOnCompletion.Count}");
            }
            else
            {
                DialogueNodeData targetNodeForReward = regSeq2.GetOrCreateEditorNode(csv_reward_node_id);
                targetNodeForReward.rewardsOnNodeCompletion.Add(reward);
                FileLogger.Log($"      Added Reward to Node '{targetNodeForReward.nodeID}': {reward.target}/{reward.stat} ({reward.amount}). Node rewards: {targetNodeForReward.rewardsOnNodeCompletion.Count}");
            }
        }
    }

    private void LinkCrossSequenceData(Dictionary<string, ScriptableObject> allSequences, string[] allCsvLines)
    {
        // Link CG node-level nextSequenceSO
        foreach (var kvp in allSequences)
        {
            if (!(kvp.Value is CGDialogueSequenceSO cgSeq)) continue;

            foreach (var node in cgSeq.nodeList)
            {
                if (!string.IsNullOrEmpty(node._editor_nextSequenceID_temp))
                {
                    if (allSequences.TryGetValue(node._editor_nextSequenceID_temp, out ScriptableObject targetSeqObj)
                        && targetSeqObj is CGDialogueSequenceSO targetCGSeq)
                    {
                        node.nextSequenceSO = targetCGSeq;
                        FileLogger.Log($"  Linked CG Node '{cgSeq.sequenceID}/{node.nodeID}' -> sequence '{targetCGSeq.sequenceID}' (node-level transition)");
                        EditorUtility.SetDirty(cgSeq);
                    }
                    else
                    {
                        FileLogger.Log($"  LinkCGNode WARNING: Target sequence '{node._editor_nextSequenceID_temp}' not found (from '{cgSeq.sequenceID}/{node.nodeID}')");
                    }

                    // Keep editor memory clean
                    node._editor_nextSequenceID_temp = null;
                    continue;
                }

                if (!string.IsNullOrEmpty(node.nextNodeID))
                {
                    bool isNodeInCurrentSequence = cgSeq.nodeList.Any(n => n.nodeID == node.nextNodeID);
                    if (!isNodeInCurrentSequence
                        && allSequences.TryGetValue(node.nextNodeID, out ScriptableObject detectedSeqObj)
                        && detectedSeqObj is CGDialogueSequenceSO detectedCGSeq)
                    {
                        string misplacedSequenceID = node.nextNodeID;
                        node.nextSequenceSO = detectedCGSeq;
                        node.nextNodeID = string.Empty;

                        FileLogger.Log($"  LinkCGNode AUTO-CORRECT: CG node '{cgSeq.sequenceID}/{node.nodeID}' had sequence ID '{misplacedSequenceID}' in NextNodeID. Auto-corrected to node-level NextSequenceID.");
                        Debug.LogWarning($"[DialogueImporter] CG node '{cgSeq.sequenceID}/{node.nodeID}': '{misplacedSequenceID}' was in NextNodeID but is a sequence ID. Auto-corrected to NextSequenceID.");
                        EditorUtility.SetDirty(cgSeq);
                    }
                }
            }
        }

        // Link CG dialogue choices (sequence navigation)
        foreach (var kvp in allSequences)
        {
            if (kvp.Value is CGDialogueSequenceSO cgSeq)
            {
                foreach (var node in cgSeq.nodeList)
                {
                    foreach (var choice in node.choices)
                    {
                        // Link nextSequenceSO from temp field
                        if (!string.IsNullOrEmpty(choice._editor_nextSequenceID_temp))
                        {
                            if (allSequences.TryGetValue(choice._editor_nextSequenceID_temp, out ScriptableObject targetSeqObj) && targetSeqObj is CGDialogueSequenceSO targetCGSeq)
                            {
                                choice.nextSequenceSO = targetCGSeq;
                                choice.startingNodeInNextSequenceID = choice._editor_startingNodeInNextSequenceID_temp;
                                FileLogger.Log($"  Linked CG Choice in '{cgSeq.sequenceID}/{node.nodeID}' -> Sequence '{targetCGSeq.sequenceID}'");
                            }
                            else
                            {
                                FileLogger.Log($"  LinkCGChoice WARNING: Target CG Sequence '{choice._editor_nextSequenceID_temp}' not found (from '{cgSeq.sequenceID}/{node.nodeID}')");
                            }
                        }
                        // Set nextNodeID for intra-sequence navigation
                        else if (!string.IsNullOrEmpty(choice._editor_nextNodeID_temp))
                        {
                            // Check if it's a node within the current sequence
                            bool isNodeInCurrentSequence = cgSeq.nodeList.Any(n => n.nodeID == choice._editor_nextNodeID_temp);

                            if (isNodeInCurrentSequence)
                            {
                                // It's a valid node ID in the current sequence
                                choice.nextNodeID = choice._editor_nextNodeID_temp;
                                FileLogger.Log($"  Linked CG Choice in '{cgSeq.sequenceID}/{node.nodeID}' -> Node '{choice.nextNodeID}' (intra-sequence)");
                            }
                            else if (allSequences.TryGetValue(choice._editor_nextNodeID_temp, out ScriptableObject detectedSeqObj) && detectedSeqObj is CGDialogueSequenceSO detectedCGSeq)
                            {
                                // It's a sequence name, not a node ID - treat it as nextSequenceSO
                                choice.nextSequenceSO = detectedCGSeq;
                                choice.startingNodeInNextSequenceID = string.Empty; // Use sequence's default starting node
                                choice.nextNodeID = string.Empty; // Clear nextNodeID since it's actually a sequence
                                FileLogger.Log($"  LinkCGChoice AUTO-CORRECT: '{choice._editor_nextNodeID_temp}' in NextNodeID column is actually a sequence name. Auto-corrected to use nextSequenceSO instead.");
                                Debug.LogWarning($"[DialogueImporter] CG Choice in sequence '{cgSeq.sequenceID}/node '{node.nodeID}': '{choice._editor_nextNodeID_temp}' was in NextNodeID column but is a sequence name. Auto-corrected to NextSequenceID.");
                            }
                            else
                            {
                                // Node ID not found in current sequence or as a sequence name - log warning
                                choice.nextNodeID = choice._editor_nextNodeID_temp;
                                FileLogger.Log($"  LinkCGChoice WARNING: NextNodeID '{choice._editor_nextNodeID_temp}' not found in sequence '{cgSeq.sequenceID}' or as a sequence name. Setting anyway.");
                                Debug.LogWarning($"[DialogueImporter] CG Choice in sequence '{cgSeq.sequenceID}/node '{node.nodeID}': NextNodeID '{choice._editor_nextNodeID_temp}' not found in current sequence.");
                            }
                        }
                    }
                }
                EditorUtility.SetDirty(cgSeq);
            }
        }

        // Link regular dialogue sequences (choices and random outcomes)
        // Only process regular dialogue sequences for cross-sequence linking
        // CG dialogue sequences don't support random outcomes
        foreach (var kvp in allSequences)
        {
            if (!(kvp.Value is DialogueSequenceSO seqSO))
                continue; // Skip CG dialogue sequences

            foreach (DialogueNodeData nodeData in seqSO.nodeList)
            {
                foreach (DialogueChoiceData choiceData in nodeData.choices)
                {
                    if (!string.IsNullOrEmpty(choiceData._editor_nextSequenceID_temp))
                    {
                        if (allSequences.TryGetValue(choiceData._editor_nextSequenceID_temp, out ScriptableObject targetSeqObj) && targetSeqObj is DialogueSequenceSO targetSeqSO)
                        {
                            choiceData.nextSequenceSO = targetSeqSO;
                            choiceData.startingNodeInNextSequenceID = choiceData._editor_startingNodeInNextSequenceID_temp;
                        }
                        else
                            FileLogger.Log($"    LinkChoice WARNING: Target SequenceSO '{choiceData._editor_nextSequenceID_temp}' not found (from '{seqSO.sequenceID}'/'{nodeData.nodeID}').");
                    }
                    else if (!string.IsNullOrEmpty(choiceData._editor_nextNodeID_temp))
                    {
                        // Check if it's a node within the current sequence
                        bool isNodeInCurrentSequence = seqSO.nodeList.Any(n => n.nodeID == choiceData._editor_nextNodeID_temp);

                        if (isNodeInCurrentSequence)
                        {
                            // It's a valid node ID in the current sequence
                            choiceData.nextNodeID = choiceData._editor_nextNodeID_temp;
                            FileLogger.Log($"    Linked Choice in '{seqSO.sequenceID}/{nodeData.nodeID}' -> Node '{choiceData.nextNodeID}' (intra-sequence)");
                        }
                        else if (allSequences.TryGetValue(choiceData._editor_nextNodeID_temp, out ScriptableObject detectedSeqObj) && detectedSeqObj is DialogueSequenceSO detectedSeqSO)
                        {
                            // It's a sequence name, not a node ID - treat it as nextSequenceSO
                            choiceData.nextSequenceSO = detectedSeqSO;
                            choiceData.startingNodeInNextSequenceID = string.Empty; // Use sequence's default starting node
                            choiceData.nextNodeID = string.Empty; // Clear nextNodeID since it's actually a sequence
                            FileLogger.Log($"    LinkChoice AUTO-CORRECT: '{choiceData._editor_nextNodeID_temp}' in NextNodeID column is actually a sequence name. Auto-corrected to use nextSequenceSO instead.");
                            Debug.LogWarning($"[DialogueImporter] Choice '{choiceData.choiceText}' in sequence '{seqSO.sequenceID}'/node '{nodeData.nodeID}': '{choiceData._editor_nextNodeID_temp}' was in NextNodeID column but is a sequence name. Auto-corrected to NextSequenceID.");
                        }
                        else
                        {
                            // Node ID not found in current sequence or as a sequence name - log warning
                            choiceData.nextNodeID = choiceData._editor_nextNodeID_temp;
                            FileLogger.Log($"    LinkChoice WARNING: NextNodeID '{choiceData._editor_nextNodeID_temp}' not found in sequence '{seqSO.sequenceID}' or as a sequence name. Setting anyway.");
                            Debug.LogWarning($"[DialogueImporter] Choice in sequence '{seqSO.sequenceID}'/node '{nodeData.nodeID}': NextNodeID '{choiceData._editor_nextNodeID_temp}' not found in current sequence.");
                        }
                    }
                }
            }

            if (seqSO.type == SequenceType.RandomOutcome)
            {
                FileLogger.Log($"  Linking Random Outcomes for Sequence: '{seqSO.sequenceID}'");
                string originalCsvLine = FindCsvLineForSequenceStart(allCsvLines, seqSO.sequenceID);

                if (!string.IsNullOrEmpty(originalCsvLine))
                {
                    int expectedCols = isMultiLanguageCSV ? EXPECTED_COLUMNS_REGULAR_MULTILANG : EXPECTED_COLUMNS_REGULAR_LEGACY;
                    string[] cells = ParseCsvLine(originalCsvLine, expectedCols);
                    string goodID = GetCellContent(cells, 17); string neutralID = GetCellContent(cells, 18); string badID = GetCellContent(cells, 19); // Shifted +2 for FontStyle + IsMonologue columns

                    if (!string.IsNullOrEmpty(goodID) && allSequences.TryGetValue(goodID, out var gSObj) && gSObj is DialogueSequenceSO gS)
                        seqSO.goodOutcomeSequence = gS;
                    else if (!string.IsNullOrEmpty(goodID))
                        FileLogger.Log($"    LinkRandom WARNING: GoodID '{goodID}' not found for '{seqSO.sequenceID}'.");

                    if (!string.IsNullOrEmpty(neutralID) && allSequences.TryGetValue(neutralID, out var nSObj) && nSObj is DialogueSequenceSO nS)
                        seqSO.neutralOutcomeSequence = nS;
                    else if (!string.IsNullOrEmpty(neutralID))
                        FileLogger.Log($"    LinkRandom WARNING: NeutralID '{neutralID}' not found for '{seqSO.sequenceID}'.");

                    if (!string.IsNullOrEmpty(badID) && allSequences.TryGetValue(badID, out var bSObj) && bSObj is DialogueSequenceSO bS)
                        seqSO.badOutcomeSequence = bS;
                    else if (!string.IsNullOrEmpty(badID))
                        FileLogger.Log($"    LinkRandom WARNING: BadID '{badID}' not found for '{seqSO.sequenceID}'.");
                }
                else
                    FileLogger.Log($"    LinkRandom WARNING: Could not find original CSV line for RandomOutcome Seq '{seqSO.sequenceID}'.");
            }
            EditorUtility.SetDirty(seqSO);
        }
    }

    private string[] ParseCsvLine(string line, int expectedFieldCount)
    {
        List<string> fields = new List<string>();
        StringBuilder currentField = new StringBuilder();
        bool inQuotes = false;
        bool fieldPossiblyStarted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            fieldPossiblyStarted = true;

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                        inQuotes = false;
                }
                else
                    currentField.Append(c);
            }
            else
            {
                if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                    fieldPossiblyStarted = false;
                }
                else
                    currentField.Append(c);
            }
        }

        if (fieldPossiblyStarted || fields.Count < expectedFieldCount)
            fields.Add(currentField.ToString().Trim());

        // Only adjust field count if expectedFieldCount is specified (not -1)
        if (expectedFieldCount != -1)
        {
            if (fields.Count < expectedFieldCount)
            {
                while (fields.Count < expectedFieldCount)
                    fields.Add(string.Empty);
            }
            else if (fields.Count > expectedFieldCount)
            {
                FileLogger.Log($"[ParseCsvLine] TRUNCATING from {fields.Count} to {expectedFieldCount} for line: [{line}]");
                fields = fields.GetRange(0, expectedFieldCount);
            }
        }

        return fields.ToArray();
    }

    private ScriptableObject CreateOrGetSequenceSO(string sequenceID)
    {
        // Determine folder path and ScriptableObject type based on dialogue type
        string folderPath;
        string assetPath;

        if (isCGDialogue)
        {
            // CG dialogue goes to Assets/Resources/Sequences/CG/
            folderPath = "Assets/Resources/Sequences/CG";
            Directory.CreateDirectory(folderPath);
            assetPath = $"{folderPath}/{sequenceID}.asset";

            // Try to load existing CGDialogueSequenceSO
            CGDialogueSequenceSO cgSO = AssetDatabase.LoadAssetAtPath<CGDialogueSequenceSO>(assetPath);

            if (cgSO == null)
            {
                FileLogger.Log($"  Creating NEW CGDialogueSequenceSO: {assetPath}");
                cgSO = ScriptableObject.CreateInstance<CGDialogueSequenceSO>();
                AssetDatabase.CreateAsset(cgSO, assetPath);
                cgSO.sequenceID = sequenceID;
                // CRITICAL: Mark new asset as dirty immediately
                EditorUtility.SetDirty(cgSO);
                AssetDatabase.SaveAssetIfDirty(cgSO);
            }
            else
            {
                FileLogger.Log($"  Loading EXISTING CGDialogueSequenceSO: {assetPath}. Clearing list data.");
                cgSO.nodeList.Clear();
                cgSO.rewardsOnCompletion.Clear();
                cgSO.startingNodeID = null;
                cgSO.sequenceID = sequenceID;
                // CRITICAL: Mark existing asset as dirty after clearing data
                EditorUtility.SetDirty(cgSO);
            }

            // Return as ScriptableObject (common base type)
            return cgSO;
        }
        else
        {
            // Regular dialogue goes to Assets/Resources/Sequences/
            folderPath = "Assets/Resources/Sequences";
            Directory.CreateDirectory(folderPath);
            assetPath = $"{folderPath}/{sequenceID}.asset";

            // Try to load existing DialogueSequenceSO
            DialogueSequenceSO so = AssetDatabase.LoadAssetAtPath<DialogueSequenceSO>(assetPath);

            if (so == null)
            {
                FileLogger.Log($"  Creating NEW DialogueSequenceSO: {assetPath}");
                so = ScriptableObject.CreateInstance<DialogueSequenceSO>();
                AssetDatabase.CreateAsset(so, assetPath);
                so.sequenceID = sequenceID;
                so.type = SequenceType.Linear;
                // CRITICAL: Mark new asset as dirty immediately
                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssetIfDirty(so);
            }
            else
            {
                FileLogger.Log($"  Loading EXISTING DialogueSequenceSO: {assetPath}. Clearing list data.");
                so.nodeList.Clear();
                so.rewardsOnCompletion.Clear();
                so.startingNodeID = null;
                so.goodOutcomeSequence = null; so.neutralOutcomeSequence = null; so.badOutcomeSequence = null;
                so.sequenceID = sequenceID;
                so.type = SequenceType.Linear;
                // CRITICAL: Mark existing asset as dirty after clearing data
                EditorUtility.SetDirty(so);
            }

            return so;
        }
    }

    private string GetCellContent(string[] cells, int index)
    {
        if (index < 0 || index >= cells.Length)
            return string.Empty;

        return cells[index];
    }

    // Debug version to log column contents for specific rows
    private string GetCellContentWithDebug(string[] cells, int index, int rowNum, string purpose)
    {
        if (index < 0 || index >= cells.Length)
        {
            FileLogger.Log($"    DEBUG: Row {rowNum} {purpose} - Column {index} out of bounds (array length: {cells.Length})");
            return string.Empty;
        }

        string content = cells[index];
        if (rowNum <= 5) // Only log first 5 rows to avoid spam
        {
            FileLogger.Log($"    DEBUG: Row {rowNum} {purpose} - Column {index}: '{content}'");
        }
        return content;
    }

    private string FindCsvLineForSequenceStart(string[] allCsvLines, string sequenceIdToFind)
    {
        for (int i = 1; i < allCsvLines.Length; i++)
        {
            string line = allCsvLines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cells = ParseCsvLine(line, EXPECTED_COLUMNS_REGULAR_LEGACY);

            if (GetCellContent(cells, 0) == sequenceIdToFind && GetCellContent(cells, 1).ToUpper() == "SEQUENCE_START")
                return line;
        }
        return null;
    }

    private string MapSpeakerName(string csvSpeakerValue)
    {
        if (string.IsNullOrEmpty(csvSpeakerValue))
            return "Mai"; // Default fallback

        // Map CSV speaker values to RewardTarget enum values
        switch (csvSpeakerValue.ToLower())
        {
            case "currentboss":
            case "mai":
            case "boss":
                return "Mai";
            case "player":
                return "Player";
            default:
                FileLogger.Log($"    WARNING: Unknown speaker value '{csvSpeakerValue}', mapping to Mai");
                return "Mai";
        }
    }

    private string MapRewardTarget(string csvRewardTargetValue)
    {
        if (string.IsNullOrEmpty(csvRewardTargetValue))
            return "Player"; // Default fallback

        // Map CSV reward target values to RewardTarget enum values
        switch (csvRewardTargetValue.ToLower())
        {
            case "currentboss":
            case "mai":
            case "boss":
                return "Mai";
            case "player":
                return "Player";
            default:
                FileLogger.Log($"    WARNING: Unknown reward target value '{csvRewardTargetValue}', mapping to Player");
                return "Player";
        }
    }

    private bool ShouldAssignLineLocalizationKey(string localizationKey, string inlineText)
    {
        if (!string.IsNullOrWhiteSpace(inlineText))
            return true;

        return FallbackDialogueLocalizationKeyExists(localizationKey);
    }

    private bool FallbackDialogueLocalizationKeyExists(string localizationKey)
    {
        if (string.IsNullOrWhiteSpace(localizationKey))
            return false;

        fallbackDialogueLocalizationKeys ??= LoadFallbackDialogueLocalizationKeys();
        return fallbackDialogueLocalizationKeys.Contains(localizationKey);
    }

    private HashSet<string> LoadFallbackDialogueLocalizationKeys()
    {
        var keys = new HashSet<string>();

        if (!File.Exists(DialogueFallbackCsvPath))
        {
            FileLogger.Log($"[DialogueImporter] Fallback dialogue localization CSV not found at '{DialogueFallbackCsvPath}'. Empty inline dialogue rows will not receive line localization keys.");
            return keys;
        }

        string[] lines = File.ReadAllLines(DialogueFallbackCsvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cells = ParseCsvLine(lines[i], -1);
            string key = GetCellContent(cells, 0);
            if (!string.IsNullOrWhiteSpace(key))
                keys.Add(key);
        }

        return keys;
    }

    private string Summarize(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "EMPTY";

        return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
    }

    private string DescribeInlineText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "externalized";

        return $"deprecated-inline '{Summarize(text, 30)}'";
    }
}
