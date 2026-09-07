using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

[System.Serializable]
public class ValidationIssue
{
    public string severity; // "Critical", "Warning", "Info"
    public string category; // "Missing Functionality", "Parameter Mismatch", "Target Missing", etc.
    public string buttonName;
    public string description;
    public string recommendation;
    public string originalTarget;
    public string originalMethod;
}

[System.Serializable]
public class MigrationValidationReport
{
    public string reportTime;
    public string sceneName;
    public int totalButtonsAnalyzed;
    public int criticalIssues;
    public int warnings;
    public int successfulMigrations;
    public List<ValidationIssue> issues = new List<ValidationIssue>();
    public List<string> missingComponents = new List<string>();
    public List<string> missingMethods = new List<string>();
    public List<string> unreachableTargets = new List<string>();
}

public class ButtonMigrationValidator : EditorWindow
{
    private MigrationValidationReport validationReport;
    private Vector2 scrollPosition;
    private string exportedFilePath = "";
    private ButtonMappingData originalMappingData;
    private bool showOnlyCritical = false;
    private bool showWarnings = true;
    private bool showInfo = true;
    
    [MenuItem("Tools/Button Migration Validator")]
    public static void ShowWindow()
    {
        GetWindow<ButtonMigrationValidator>("Migration Validator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Button Migration Validator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Exported File", GUILayout.Height(30)))
        {
            LoadExportedFile();
        }
        if (GUILayout.Button("Validate Current Scene", GUILayout.Height(30)))
        {
            ValidateCurrentScene();
        }
        if (GUILayout.Button("Detailed Scene Analysis", GUILayout.Height(30)))
        {
            PerformDetailedSceneAnalysis();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(exportedFilePath))
        {
            EditorGUILayout.LabelField($"Loaded File: {Path.GetFileName(exportedFilePath)}", EditorStyles.helpBox);
        }

        EditorGUILayout.Space();

        // Filter options
        EditorGUILayout.BeginHorizontal();
        showOnlyCritical = EditorGUILayout.Toggle("Show Only Critical", showOnlyCritical);
        showWarnings = EditorGUILayout.Toggle("Show Warnings", showWarnings);
        showInfo = EditorGUILayout.Toggle("Show Info", showInfo);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (validationReport != null)
        {
            DrawValidationReport();
        }
        else
        {
            EditorGUILayout.HelpBox("Load an exported UIManager_ButtonMethods.txt file and validate the current scene to check migration compatibility.", MessageType.Info);
        }
    }

    private void LoadExportedFile()
    {
        string filePath = EditorUtility.OpenFilePanel("Load UIManager Button Methods File", Application.dataPath, "txt");
        if (!string.IsNullOrEmpty(filePath))
        {
            exportedFilePath = filePath;
            
            // Also load the original mapping data if available
            LoadOriginalMappingData();
            
            EditorUtility.DisplayDialog("File Loaded", $"Loaded: {Path.GetFileName(filePath)}\nNow click 'Validate Current Scene' to perform migration validation.", "OK");
        }
    }

    private void LoadOriginalMappingData()
    {
        // Try to scan current scene to get original mapping data
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (!currentScene.IsValid()) return;

        originalMappingData = new ButtonMappingData
        {
            sceneName = currentScene.name,
            scanTime = DateTime.Now
        };

        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in allButtons)
        {
            var mapping = AnalyzeButtonForValidation(button);
            originalMappingData.buttonMappings.Add(mapping);
        }
    }

    private ButtonOnClickMapping AnalyzeButtonForValidation(Button button)
    {
        // Simplified version of the mapping analysis for validation purposes
        var mapping = new ButtonOnClickMapping
        {
            buttonName = button.name,
            gameObjectPath = GetGameObjectPath(button.transform),
            buttonType = PrefabUtility.IsPartOfPrefabInstance(button) ? "Prefab Button" : "Scene Button"
        };

        bool hasPersistentListeners = button.onClick.GetPersistentEventCount() > 0;
        if (hasPersistentListeners)
        {
            mapping.hasListener = true;
            UnityEngine.Object target = button.onClick.GetPersistentTarget(0);
            string methodName = button.onClick.GetPersistentMethodName(0);
            
            mapping.targetObjectName = target != null ? target.name : "Unknown";
            mapping.methodName = methodName;

            // Get basic parameter info for validation including target paths
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                string targetPath = "Unknown";
                if (target != null)
                {
                    if (target is Component comp)
                        targetPath = GetGameObjectPath(comp.transform);
                    else if (target is GameObject go)
                        targetPath = GetGameObjectPath(go.transform);
                    else
                        targetPath = "ScriptableObject Asset";
                }

                var paramInfo = new ButtonParameterInfo
                {
                    parameterType = "Persistent",
                    targetGameObjectName = mapping.targetObjectName,
                    targetGameObjectPath = targetPath,
                    targetComponentType = target != null ? target.GetType().Name : "Unknown",
                    methodSignature = methodName,
                    listenerIndex = i
                };
                mapping.parameters.Add(paramInfo);
            }
        }

        return mapping;
    }

    private void ValidateCurrentScene()
    {
        if (string.IsNullOrEmpty(exportedFilePath))
        {
            EditorUtility.DisplayDialog("No File Loaded", "Please load an exported UIManager_ButtonMethods.txt file first.", "OK");
            return;
        }

        validationReport = new MigrationValidationReport
        {
            reportTime = DateTime.Now.ToString(),
            sceneName = EditorSceneManager.GetActiveScene().name
        };

        // Parse the exported file
        var exportedMethods = ParseExportedFile(exportedFilePath);
        
        // Get current scene button data
        if (originalMappingData == null)
            LoadOriginalMappingData();

        // Perform validation checks
        PerformValidationChecks(exportedMethods);

        Debug.Log($"Migration validation complete. Found {validationReport.criticalIssues} critical issues, {validationReport.warnings} warnings.");
        Repaint();
    }

    private void PerformDetailedSceneAnalysis()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (!currentScene.IsValid())
        {
            EditorUtility.DisplayDialog("Invalid Scene", "No valid scene is currently open!", "OK");
            return;
        }

        var analysisReport = new System.Text.StringBuilder();
        analysisReport.AppendLine($"=== Detailed Scene Analysis for '{currentScene.name}' ===");
        analysisReport.AppendLine($"Analysis Time: {DateTime.Now}");
        analysisReport.AppendLine();

        // Analyze all GameObjects including inactive
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene.IsValid())
            .OrderBy(go => go.name)
            .ToArray();

        analysisReport.AppendLine($"Total GameObjects in Scene: {allObjects.Length}");
        
        var activeObjects = allObjects.Where(go => go.activeInHierarchy).ToArray();
        var inactiveObjects = allObjects.Where(go => !go.activeInHierarchy).ToArray();
        
        analysisReport.AppendLine($"Active GameObjects: {activeObjects.Length}");
        analysisReport.AppendLine($"Inactive GameObjects: {inactiveObjects.Length}");
        analysisReport.AppendLine();

        // Analyze buttons
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>()
            .Where(b => b.gameObject.scene.IsValid())
            .ToArray();
            
        analysisReport.AppendLine($"=== BUTTON ANALYSIS ===");
        analysisReport.AppendLine($"Total Buttons Found: {allButtons.Length}");
        
        var activeButtons = allButtons.Where(b => b.gameObject.activeInHierarchy).ToArray();
        var inactiveButtons = allButtons.Where(b => !b.gameObject.activeInHierarchy).ToArray();
        
        analysisReport.AppendLine($"Active Buttons: {activeButtons.Length}");
        analysisReport.AppendLine($"Inactive Buttons: {inactiveButtons.Length}");
        analysisReport.AppendLine();

        // Analyze button listeners
        var buttonsWithPersistentListeners = allButtons.Where(b => b.onClick.GetPersistentEventCount() > 0).ToArray();
        analysisReport.AppendLine($"Buttons with Persistent Listeners: {buttonsWithPersistentListeners.Length}");
        
        // Check for duplicate GameObject names
        var duplicateNames = allObjects.GroupBy(go => go.name)
            .Where(g => g.Count() > 1)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (duplicateNames.Any())
        {
            analysisReport.AppendLine();
            analysisReport.AppendLine($"=== DUPLICATE GAMEOBJECT NAMES ===");
            foreach (var duplicate in duplicateNames)
            {
                analysisReport.AppendLine($"  '{duplicate.Name}': {duplicate.Count} objects");
            }
        }

        // Analyze component types
        var allComponentTypes = new HashSet<string>();
        foreach (var go in allObjects)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null)
                    allComponentTypes.Add(comp.GetType().Name);
            }
        }

        analysisReport.AppendLine();
        analysisReport.AppendLine($"=== COMPONENT TYPES IN SCENE ===");
        analysisReport.AppendLine($"Total Unique Component Types: {allComponentTypes.Count}");
        
        var sortedComponents = allComponentTypes.OrderBy(c => c).ToList();
        foreach (var componentType in sortedComponents)
        {
            analysisReport.AppendLine($"  - {componentType}");
        }

        // Show the analysis in a scrollable dialog
        string reportPath = EditorUtility.SaveFilePanel("Save Scene Analysis", Application.dataPath, $"SceneAnalysis_{currentScene.name}", "txt");
        if (!string.IsNullOrEmpty(reportPath))
        {
            File.WriteAllText(reportPath, analysisReport.ToString());
            Debug.Log($"Detailed scene analysis saved to: {reportPath}");
            EditorUtility.DisplayDialog("Analysis Complete", $"Detailed scene analysis saved to:\n{reportPath}", "OK");
        }
    }

    private Dictionary<string, ExportedMethodInfo> ParseExportedFile(string filePath)
    {
        var methods = new Dictionary<string, ExportedMethodInfo>();
        
        try
        {
            string content = File.ReadAllText(filePath);
            
            // Parse method blocks using regex
            var methodPattern = @"public void (On\w+Click)\(\)\s*\{(.*?)\}";
            var matches = Regex.Matches(content, methodPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                string methodName = match.Groups[1].Value;
                string methodBody = match.Groups[2].Value;
                
                var methodInfo = new ExportedMethodInfo
                {
                    methodName = methodName,
                    methodBody = methodBody,
                    buttonName = ExtractButtonNameFromComments(methodBody),
                    originalTarget = ExtractOriginalTarget(methodBody),
                    originalMethod = ExtractOriginalMethod(methodBody),
                    hasImplementation = !methodBody.Contains("// TODO: Implement the centralized button logic here"),
                    persistentListeners = ExtractPersistentListeners(methodBody),
                    runtimeListeners = ExtractRuntimeListeners(methodBody)
                };
                
                methods[methodName] = methodInfo;
            }
        }
        catch (Exception ex)
        {
            validationReport.issues.Add(new ValidationIssue
            {
                severity = "Critical",
                category = "File Parsing",
                description = $"Failed to parse exported file: {ex.Message}",
                recommendation = "Check if the exported file is properly formatted."
            });
        }
        
        return methods;
    }

    private void PerformValidationChecks(Dictionary<string, ExportedMethodInfo> exportedMethods)
    {
        var buttonsWithListeners = originalMappingData.buttonMappings.Where(m => m.hasListener).ToList();
        validationReport.totalButtonsAnalyzed = buttonsWithListeners.Count;

        foreach (var buttonMapping in buttonsWithListeners)
        {
            ValidateButtonMigration(buttonMapping, exportedMethods);
        }

        // Check for orphaned exported methods
        CheckForOrphanedMethods(exportedMethods, buttonsWithListeners);

        // Validate component availability
        ValidateComponentAvailability();

        // Calculate summary statistics
        validationReport.criticalIssues = validationReport.issues.Count(i => i.severity == "Critical");
        validationReport.warnings = validationReport.issues.Count(i => i.severity == "Warning");
        validationReport.successfulMigrations = validationReport.totalButtonsAnalyzed - validationReport.criticalIssues;
    }

    private void ValidateButtonMigration(ButtonOnClickMapping buttonMapping, Dictionary<string, ExportedMethodInfo> exportedMethods)
    {
        string expectedMethodName = GenerateMethodName(buttonMapping.buttonName);
        
        // Check if exported method exists
        if (!exportedMethods.ContainsKey(expectedMethodName))
        {
            validationReport.issues.Add(new ValidationIssue
            {
                severity = "Critical",
                category = "Missing Method",
                buttonName = $"{buttonMapping.buttonName} @ {buttonMapping.gameObjectPath}",
                description = $"No exported method found for button '{buttonMapping.buttonName}' at path '{buttonMapping.gameObjectPath}'",
                recommendation = $"Expected method '{expectedMethodName}' was not found in exported file.",
                originalTarget = buttonMapping.targetObjectName,
                originalMethod = buttonMapping.methodName
            });
            return;
        }

        var exportedMethod = exportedMethods[expectedMethodName];

        // Check if method has implementation
        if (!exportedMethod.hasImplementation)
        {
            validationReport.issues.Add(new ValidationIssue
            {
                severity = "Warning",
                category = "No Implementation",
                buttonName = $"{buttonMapping.buttonName} @ {buttonMapping.gameObjectPath}",
                description = $"Method '{expectedMethodName}' for button '{buttonMapping.buttonName}' at path '{buttonMapping.gameObjectPath}' exists but has no implementation (still contains TODO)",
                recommendation = "Implement the button logic to replace the original functionality.",
                originalTarget = buttonMapping.targetObjectName,
                originalMethod = buttonMapping.methodName
            });
        }

        // Validate original target accessibility
        ValidateOriginalTarget(buttonMapping, exportedMethod);

        // Validate parameter preservation
        ValidateParameterPreservation(buttonMapping, exportedMethod);

        // Check for component references
        ValidateComponentReferences(buttonMapping, exportedMethod);
    }

    private void ValidateOriginalTarget(ButtonOnClickMapping buttonMapping, ExportedMethodInfo exportedMethod)
    {
        if (string.IsNullOrEmpty(buttonMapping.targetObjectName) || buttonMapping.targetObjectName == "Unknown")
            return;

        // Try to find the original target GameObject in the scene (including inactive)
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene.IsValid()) // Only scene objects, not assets
            .ToArray();
            
        bool targetExists = allObjects.Any(go => go.name == buttonMapping.targetObjectName);

        if (!targetExists)
        {
            // Also check if it's a singleton/manager that might be named differently
            bool hasManagerComponent = CheckForManagerComponents(buttonMapping.targetObjectName, allObjects);
            
            if (!hasManagerComponent)
            {
                validationReport.issues.Add(new ValidationIssue
                {
                    severity = "Critical",
                    category = "Missing Target",
                    buttonName = $"{buttonMapping.buttonName} @ {buttonMapping.gameObjectPath}",
                    description = $"Original target GameObject '{buttonMapping.targetObjectName}' not found in scene (checked {allObjects.Length} objects including inactive). Button is at path '{buttonMapping.gameObjectPath}'",
                    recommendation = "Ensure the target GameObject exists or update the implementation to use available components.",
                    originalTarget = buttonMapping.targetObjectName,
                    originalMethod = buttonMapping.methodName
                });
                
                if (!validationReport.unreachableTargets.Contains(buttonMapping.targetObjectName))
                    validationReport.unreachableTargets.Add(buttonMapping.targetObjectName);
            }
        }
        else
        {
            // Additional validation: check if target is inactive and might cause runtime issues
            var targetObjects = allObjects.Where(go => go.name == buttonMapping.targetObjectName).ToArray();
            bool allInactive = targetObjects.All(go => !go.activeInHierarchy);
            
            if (allInactive)
            {
                validationReport.issues.Add(new ValidationIssue
                {
                    severity = "Warning",
                    category = "Inactive Target",
                    buttonName = $"{buttonMapping.buttonName} @ {buttonMapping.gameObjectPath}",
                    description = $"Target GameObject '{buttonMapping.targetObjectName}' exists but is inactive. Button is at path '{buttonMapping.gameObjectPath}'",
                    recommendation = "Ensure the target GameObject is active when button interactions occur, or handle inactive state in implementation.",
                    originalTarget = buttonMapping.targetObjectName,
                    originalMethod = buttonMapping.methodName
                });
            }
        }
    }

    private bool CheckForManagerComponents(string targetName, GameObject[] allObjects)
    {
        // Check for common manager patterns that might be named differently
        string[] managerPatterns = { "Manager", "Controller", "System", "Service" };
        
        foreach (var pattern in managerPatterns)
        {
            if (targetName.Contains(pattern))
            {
                // Look for any GameObject with components that match the pattern
                foreach (var obj in allObjects)
                {
                    var components = obj.GetComponents<Component>();
                    if (components.Any(c => c != null && c.GetType().Name.Contains(pattern)))
                    {
                        return true; // Found a potential manager
                    }
                }
            }
        }
        
        return false;
    }

    private void ValidateParameterPreservation(ButtonOnClickMapping buttonMapping, ExportedMethodInfo exportedMethod)
    {
        var persistentParams = buttonMapping.parameters.Where(p => p.parameterType == "Persistent").ToList();
        
        foreach (var param in persistentParams)
        {
            if (!string.IsNullOrEmpty(param.parameterValue) && param.parameterValue != "Void")
            {
                // Check if the parameter value is mentioned in the exported method comments
                bool parameterDocumented = exportedMethod.methodBody.Contains(param.parameterValue);
                
                if (!parameterDocumented)
                {
                    validationReport.issues.Add(new ValidationIssue
                    {
                        severity = "Warning",
                        category = "Parameter Not Documented",
                        buttonName = $"{buttonMapping.buttonName} @ {buttonMapping.gameObjectPath}",
                        description = $"Parameter value '{param.parameterValue}' from {param.targetComponentType}.{param.methodSignature} is not documented in exported method for button at path '{buttonMapping.gameObjectPath}'",
                        recommendation = "Ensure this parameter value is considered in the implementation.",
                        originalTarget = param.targetGameObjectName,
                        originalMethod = param.methodSignature
                    });
                }
            }
        }
    }

    private void ValidateComponentReferences(ButtonOnClickMapping buttonMapping, ExportedMethodInfo exportedMethod)
    {
        var componentTypes = buttonMapping.parameters
            .Where(p => !string.IsNullOrEmpty(p.targetComponentType))
            .Select(p => p.targetComponentType)
            .Distinct()
            .ToList();

        foreach (var componentType in componentTypes)
        {
            if (componentType == "Unknown") continue;

            // Check if component type exists in the project
            Type type = GetTypeFromName(componentType);
            if (type == null)
            {
                // Additional check: look for the component in the scene objects
                bool componentExistsInScene = CheckComponentExistsInScene(componentType);
                
                if (!componentExistsInScene)
                {
                    validationReport.issues.Add(new ValidationIssue
                    {
                        severity = "Critical",
                        category = "Missing Component Type",
                        buttonName = $"{buttonMapping.buttonName} @ {buttonMapping.gameObjectPath}",
                        description = $"Component type '{componentType}' not found in project assemblies or scene objects. Button is at path '{buttonMapping.gameObjectPath}'",
                        recommendation = "Ensure the component script exists, is properly compiled, and not in a different namespace.",
                        originalTarget = buttonMapping.targetObjectName,
                        originalMethod = buttonMapping.methodName
                    });
                    
                    if (!validationReport.missingComponents.Contains(componentType))
                        validationReport.missingComponents.Add(componentType);
                }
                else
                {
                    // Component exists in scene but type lookup failed - likely namespace issue
                    validationReport.issues.Add(new ValidationIssue
                    {
                        severity = "Warning",
                        category = "Component Type Resolution",
                        buttonName = $"{buttonMapping.buttonName} @ {buttonMapping.gameObjectPath}",
                        description = $"Component '{componentType}' exists in scene but type resolution failed (possibly namespace issue). Button is at path '{buttonMapping.gameObjectPath}'",
                        recommendation = "Component is functional but may have namespace or assembly reference issues.",
                        originalTarget = buttonMapping.targetObjectName,
                        originalMethod = buttonMapping.methodName
                    });
                }
            }
        }
    }

    private bool CheckComponentExistsInScene(string componentTypeName)
    {
        // Get all GameObjects in scene including inactive ones
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene.IsValid())
            .ToArray();

        foreach (var go in allObjects)
        {
            var components = go.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component != null && component.GetType().Name == componentTypeName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CheckForOrphanedMethods(Dictionary<string, ExportedMethodInfo> exportedMethods, List<ButtonOnClickMapping> buttonsWithListeners)
    {
        var expectedMethods = buttonsWithListeners.Select(b => GenerateMethodName(b.buttonName)).ToHashSet();
        
        foreach (var exportedMethod in exportedMethods.Keys)
        {
            if (!expectedMethods.Contains(exportedMethod))
            {
                validationReport.issues.Add(new ValidationIssue
                {
                    severity = "Info",
                    category = "Orphaned Method",
                    description = $"Exported method '{exportedMethod}' doesn't correspond to any button in current scene",
                    recommendation = "This method might be from a different scene or can be removed if not needed."
                });
            }
        }
    }

    private void ValidateComponentAvailability()
    {
        // Check if UIManager exists and has the required structure
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            validationReport.issues.Add(new ValidationIssue
            {
                severity = "Critical",
                category = "Missing UIManager",
                description = "UIManager component not found in scene",
                recommendation = "Add UIManager component to the scene or update the exported methods to use the correct manager class."
            });
        }

        // Check if GameManager is accessible (commonly used in button logic)
        if (GameManager.Instance == null)
        {
            validationReport.issues.Add(new ValidationIssue
            {
                severity = "Warning",
                category = "Missing GameManager",
                description = "GameManager.Instance is null - this might cause runtime errors",
                recommendation = "Ensure GameManager is properly initialized before button interactions."
            });
        }
    }

    private void DrawValidationReport()
    {
        // Summary section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Validation Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Scene: {validationReport.sceneName}");
        EditorGUILayout.LabelField($"Report Time: {validationReport.reportTime}");
        EditorGUILayout.LabelField($"Total Buttons Analyzed: {validationReport.totalButtonsAnalyzed}");
        
        var oldColor = GUI.color;
        
        GUI.color = validationReport.criticalIssues > 0 ? Color.red : Color.green;
        EditorGUILayout.LabelField($"Critical Issues: {validationReport.criticalIssues}");
        
        GUI.color = validationReport.warnings > 0 ? Color.yellow : Color.green;
        EditorGUILayout.LabelField($"Warnings: {validationReport.warnings}");
        
        GUI.color = Color.green;
        EditorGUILayout.LabelField($"Successful Migrations: {validationReport.successfulMigrations}");
        
        GUI.color = oldColor;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Migration readiness assessment
        float migrationReadiness = (float)validationReport.successfulMigrations / validationReport.totalButtonsAnalyzed * 100f;
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Migration Readiness Assessment", EditorStyles.boldLabel);
        
        oldColor = GUI.color;
        if (migrationReadiness >= 90f) GUI.color = Color.green;
        else if (migrationReadiness >= 70f) GUI.color = Color.yellow;
        else GUI.color = Color.red;
        
        EditorGUILayout.LabelField($"Readiness: {migrationReadiness:F1}%");
        GUI.color = oldColor;
        
        if (validationReport.criticalIssues == 0)
        {
            EditorGUILayout.HelpBox("✓ Migration appears safe to proceed. All critical issues resolved.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"⚠ {validationReport.criticalIssues} critical issues must be resolved before migration.", MessageType.Error);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Issues list
        var filteredIssues = validationReport.issues.Where(i => 
            (showOnlyCritical && i.severity == "Critical") ||
            (!showOnlyCritical && (
                (i.severity == "Critical") ||
                (i.severity == "Warning" && showWarnings) ||
                (i.severity == "Info" && showInfo)
            ))
        ).ToList();

        EditorGUILayout.LabelField($"Issues ({filteredIssues.Count}):", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var issue in filteredIssues)
        {
            DrawValidationIssue(issue);
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawValidationIssue(ValidationIssue issue)
    {
        var oldColor = GUI.color;
        
        switch (issue.severity)
        {
            case "Critical": GUI.color = Color.red; break;
            case "Warning": GUI.color = Color.yellow; break;
            case "Info": GUI.color = Color.cyan; break;
        }
        
        EditorGUILayout.BeginVertical("box");
        GUI.color = oldColor;
        
        EditorGUILayout.LabelField($"[{issue.severity}] {issue.category}", EditorStyles.boldLabel);
        
        if (!string.IsNullOrEmpty(issue.buttonName))
        {
            // Parse button name and path if they're combined
            if (issue.buttonName.Contains(" @ "))
            {
                var parts = issue.buttonName.Split(new string[] { " @ " }, StringSplitOptions.None);
                if (parts.Length >= 2)
                {
                    EditorGUILayout.LabelField($"Button Name: {parts[0]}", EditorStyles.label);
                    EditorGUILayout.LabelField($"Button Path: {parts[1]}", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"Button: {issue.buttonName}");
                }
            }
            else
            {
                EditorGUILayout.LabelField($"Button: {issue.buttonName}");
            }
        }
            
        if (!string.IsNullOrEmpty(issue.originalTarget))
            EditorGUILayout.LabelField($"Original Target: {issue.originalTarget}");
            
        if (!string.IsNullOrEmpty(issue.originalMethod))
            EditorGUILayout.LabelField($"Original Method: {issue.originalMethod}");
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Issue:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(issue.description, EditorStyles.wordWrappedLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recommendation:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(issue.recommendation, EditorStyles.wordWrappedMiniLabel);
        
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    // Helper methods
    private string GenerateMethodName(string buttonName)
    {
        string methodName = buttonName.Replace(" ", "").Replace("_", "").Replace("-", "");
        methodName = "On" + char.ToUpper(methodName[0]) + methodName.Substring(1) + "Click";
        return methodName;
    }

    private string GetGameObjectPath(Transform transform)
    {
        List<string> pathComponents = new List<string>();
        Transform current = transform;
        
        while (current != null)
        {
            // Handle duplicate names by adding instance ID for uniqueness
            string nodeName = current.name;
            
            // Check for siblings with same name
            if (current.parent != null)
            {
                var siblingsWithSameName = new List<Transform>();
                for (int i = 0; i < current.parent.childCount; i++)
                {
                    var sibling = current.parent.GetChild(i);
                    if (sibling.name == current.name)
                    {
                        siblingsWithSameName.Add(sibling);
                    }
                }
                
                // If there are multiple siblings with the same name, add index/ID for disambiguation
                if (siblingsWithSameName.Count > 1)
                {
                    int siblingIndex = siblingsWithSameName.IndexOf(current);
                    nodeName = $"{current.name}[{siblingIndex}]({current.GetInstanceID()})";
                }
            }
            else
            {
                // Check for root GameObjects with same name
                var rootObjectsWithSameName = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(go => go.scene.IsValid() && go.transform.parent == null && go.name == current.name)
                    .ToList();
                
                if (rootObjectsWithSameName.Count > 1)
                {
                    nodeName = $"{current.name}({current.GetInstanceID()})";
                }
            }
            
            pathComponents.Insert(0, nodeName);
            current = current.parent;
        }
        
        return string.Join("/", pathComponents);
    }

    private Type GetTypeFromName(string typeName)
    {
        // First try the current assembly with full name
        Type type = Type.GetType(typeName);
        if (type != null) return type;

        // Try all loaded assemblies with exact name
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            catch
            {
                // Continue if assembly can't be accessed
            }
        }

        // Try searching by simple name in all assemblies
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = assembly.GetTypes();
                type = types.FirstOrDefault(t => t.Name == typeName);
                if (type != null) return type;
            }
            catch
            {
                // Continue if assembly can't be accessed or types can't be loaded
            }
        }

        // Try Unity-specific type lookup
        type = GetUnityType(typeName);
        if (type != null) return type;

        // Last resort: check if it's a common Unity component
        if (IsKnownUnityComponent(typeName))
        {
            return typeof(Component); // Return base Component type as fallback
        }

        return null;
    }

    private Type GetUnityType(string typeName)
    {
        // Common Unity types that might not be found through normal reflection
        var unityTypeMap = new Dictionary<string, Type>
        {
            { "Button", typeof(Button) },
            { "Image", typeof(UnityEngine.UI.Image) },
            { "Text", typeof(UnityEngine.UI.Text) },
            { "Canvas", typeof(Canvas) },
            { "CanvasGroup", typeof(CanvasGroup) },
            { "RectTransform", typeof(RectTransform) },
            { "Transform", typeof(Transform) },
            { "GameObject", typeof(GameObject) },
            { "MonoBehaviour", typeof(MonoBehaviour) },
            { "Component", typeof(Component) },
            { "Animator", typeof(Animator) },
            { "AudioSource", typeof(AudioSource) }
        };

        return unityTypeMap.ContainsKey(typeName) ? unityTypeMap[typeName] : null;
    }

    private bool IsKnownUnityComponent(string typeName)
    {
        // List of common Unity component names that should exist
        var knownComponents = new HashSet<string>
        {
            "Button", "Image", "Text", "Canvas", "CanvasGroup", "RectTransform", 
            "Transform", "Animator", "AudioSource", "Rigidbody", "Collider",
            "Camera", "Light", "Renderer", "MeshRenderer", "SpriteRenderer"
        };

        return knownComponents.Contains(typeName);
    }

    private string ExtractButtonNameFromComments(string methodBody)
    {
        var match = Regex.Match(methodBody, @"// Button: (.+)");
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }

    private string ExtractOriginalTarget(string methodBody)
    {
        var match = Regex.Match(methodBody, @"// Primary Target: (.+)");
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }

    private string ExtractOriginalMethod(string methodBody)
    {
        var match = Regex.Match(methodBody, @"// Primary Method: (.+)");
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }

    private List<string> ExtractPersistentListeners(string methodBody)
    {
        var listeners = new List<string>();
        var matches = Regex.Matches(methodBody, @"//\s+Target: (.+) \((.+)\)");
        foreach (Match match in matches)
        {
            listeners.Add($"{match.Groups[1].Value.Trim()} ({match.Groups[2].Value.Trim()})");
        }
        return listeners;
    }

    private List<string> ExtractRuntimeListeners(string methodBody)
    {
        var listeners = new List<string>();
        if (methodBody.Contains("Runtime targets:"))
        {
            var match = Regex.Match(methodBody, @"// Runtime targets: (.+)");
            if (match.Success)
            {
                listeners.AddRange(match.Groups[1].Value.Split(',').Select(s => s.Trim()));
            }
        }
        return listeners;
    }
}

[System.Serializable]
public class ExportedMethodInfo
{
    public string methodName;
    public string methodBody;
    public string buttonName;
    public string originalTarget;
    public string originalMethod;
    public bool hasImplementation;
    public List<string> persistentListeners = new List<string>();
    public List<string> runtimeListeners = new List<string>();
}