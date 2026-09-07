using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

[System.Serializable]
public class ButtonOnClickMapping
{
    public string buttonName;
    public string gameObjectPath;
    public string targetObjectName;
    public string methodName;
    public List<ButtonParameterInfo> parameters = new List<ButtonParameterInfo>();
    public bool hasListener;
    public string buttonType; // "Scene Button" or "Prefab Button"
}

[System.Serializable]
public class ButtonParameterInfo
{
    public string parameterType; // "Persistent" or "Runtime"
    public string parameterDescription;
    public string targetGameObjectName;
    public string targetGameObjectPath;
    public string targetComponentType;
    public string methodSignature;
    public string parameterValue;
    public int listenerIndex;
}

[System.Serializable]
public class ButtonMappingData
{
    public List<ButtonOnClickMapping> buttonMappings = new List<ButtonOnClickMapping>();
    public string sceneName;
    public DateTime scanTime;
}

public class ButtonOnClickMapper : EditorWindow
{
    private ButtonMappingData mappingData;
    private Vector2 scrollPosition;
    private bool showOnlyWithListeners = true;
    private string searchFilter = "";

    [MenuItem("Tools/Button OnClick Mapper")]
    public static void ShowWindow()
    {
        GetWindow<ButtonOnClickMapper>("Button OnClick Mapper");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Button OnClick Mapper", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Current Scene", GUILayout.Height(30)))
        {
            ScanCurrentScene();
        }
        if (GUILayout.Button("Export to UIManager", GUILayout.Height(30)))
        {
            ExportToUIManager();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Filters
        EditorGUILayout.BeginHorizontal();
        showOnlyWithListeners = EditorGUILayout.Toggle("Show only buttons with listeners", showOnlyWithListeners);
        EditorGUILayout.EndHorizontal();

        searchFilter = EditorGUILayout.TextField("Search:", searchFilter);
        EditorGUILayout.Space();

        if (mappingData != null && mappingData.buttonMappings.Count > 0)
        {
            EditorGUILayout.LabelField($"Scene: {mappingData.sceneName}", EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Scan Time: {mappingData.scanTime}", EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Buttons Found: {mappingData.buttonMappings.Count}", EditorStyles.helpBox);

            var filteredMappings = mappingData.buttonMappings.Where(m => 
                (!showOnlyWithListeners || m.hasListener) &&
                (string.IsNullOrEmpty(searchFilter) || 
                 m.buttonName.ToLower().Contains(searchFilter.ToLower()) ||
                 m.gameObjectPath.ToLower().Contains(searchFilter.ToLower()) ||
                 m.methodName.ToLower().Contains(searchFilter.ToLower()))
            ).ToList();

            EditorGUILayout.LabelField($"Filtered Results: {filteredMappings.Count}", EditorStyles.helpBox);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var mapping in filteredMappings)
            {
                DrawButtonMapping(mapping);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("No button mappings found. Click 'Scan Current Scene' to analyze buttons.", MessageType.Info);
        }
    }

    private void DrawButtonMapping(ButtonOnClickMapping mapping)
    {
        EditorGUILayout.BeginVertical("box");

        // Button name and path
        EditorGUILayout.LabelField($"Button: {mapping.buttonName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Path: {mapping.gameObjectPath}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Type: {mapping.buttonType}", EditorStyles.miniLabel);

        if (mapping.hasListener)
        {
            EditorGUILayout.LabelField($"Primary Target: {mapping.targetObjectName}", EditorStyles.label);
            EditorGUILayout.LabelField($"Primary Method: {mapping.methodName}", EditorStyles.label);
            
            if (mapping.parameters.Count > 0)
            {
                EditorGUILayout.LabelField("Event Details:", EditorStyles.label);
                foreach (var param in mapping.parameters)
                {
                    if (param.parameterType == "Header")
                    {
                        var oldColor = GUI.color;
                        GUI.color = param.parameterDescription.Contains("PERSISTENT") ? Color.cyan : Color.yellow;
                        EditorGUILayout.LabelField(param.parameterDescription, EditorStyles.boldLabel);
                        GUI.color = oldColor;
                    }
                    else
                    {
                        var oldColor = GUI.color;
                        GUI.color = param.parameterType == "Persistent" ? Color.cyan : Color.yellow;
                        
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField($"  {param.parameterDescription}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"    Target GameObject: {param.targetGameObjectName}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"    GameObject Path: {param.targetGameObjectPath}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"    Component Type: {param.targetComponentType}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"    Method: {param.methodSignature}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"    Value: {param.parameterValue}", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                        
                        GUI.color = oldColor;
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No OnClick listeners", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void ScanCurrentScene()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (!currentScene.IsValid())
        {
            Debug.LogError("No valid scene is currently open!");
            return;
        }

        mappingData = new ButtonMappingData
        {
            sceneName = currentScene.name,
            scanTime = DateTime.Now
        };

        // Find all Button components in the scene
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        Debug.Log($"Found {allButtons.Length} buttons in scene '{currentScene.name}'");

        foreach (Button button in allButtons)
        {
            ButtonOnClickMapping mapping = AnalyzeButton(button);
            mappingData.buttonMappings.Add(mapping);
        }

        Debug.Log($"Button analysis complete. Found {mappingData.buttonMappings.Count(m => m.hasListener)} buttons with listeners.");
        Repaint();
    }

    private ButtonOnClickMapping AnalyzeButton(Button button)
    {
        ButtonOnClickMapping mapping = new ButtonOnClickMapping
        {
            buttonName = button.name,
            gameObjectPath = GetGameObjectPath(button.transform),
            buttonType = PrefabUtility.IsPartOfPrefabInstance(button) ? "Prefab Button" : "Scene Button"
        };

        bool hasPersistentListeners = button.onClick.GetPersistentEventCount() > 0;
        bool hasRuntimeListeners = HasRuntimeListeners(button.onClick);

        if (hasPersistentListeners || hasRuntimeListeners)
        {
            mapping.hasListener = true;
            
            // Handle persistent listeners (Inspector-assigned)
            if (hasPersistentListeners)
            {
                UnityEngine.Object target = button.onClick.GetPersistentTarget(0);
                string methodName = button.onClick.GetPersistentMethodName(0);
                
                mapping.targetObjectName = target != null ? target.name : "Unknown";
                mapping.methodName = methodName;
                
                // Add section header
                mapping.parameters.Add(new ButtonParameterInfo
                {
                    parameterType = "Header",
                    parameterDescription = "[PERSISTENT LISTENERS]",
                    listenerIndex = -1
                });

                // Get detailed persistent parameters
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    var persistentParamInfo = GetDetailedPersistentParameterInfo(button, i);
                    if (persistentParamInfo != null)
                        mapping.parameters.Add(persistentParamInfo);
                }
            }

            // Handle runtime listeners (Code-assigned)
            if (hasRuntimeListeners)
            {
                if (hasPersistentListeners)
                {
                    mapping.parameters.Add(new ButtonParameterInfo
                    {
                        parameterType = "Header",
                        parameterDescription = "[RUNTIME LISTENERS]",
                        listenerIndex = -1
                    });
                }
                    
                var runtimeParamInfos = GetDetailedRuntimeListenerInfo(button.onClick);
                if (runtimeParamInfos.Count > 0)
                {
                    if (!hasPersistentListeners && runtimeParamInfos.Count > 0)
                    {
                        mapping.targetObjectName = runtimeParamInfos[0].targetGameObjectName;
                        mapping.methodName = runtimeParamInfos[0].methodSignature;
                    }
                    
                    foreach (var runtimeParam in runtimeParamInfos)
                    {
                        mapping.parameters.Add(runtimeParam);
                    }
                }
            }
        }
        else
        {
            mapping.hasListener = false;
            mapping.targetObjectName = "None";
            mapping.methodName = "None";
        }

        return mapping;
    }

    private bool HasRuntimeListeners(Button.ButtonClickedEvent buttonEvent)
    {
        try
        {
            // Use reflection to check if there are runtime listeners
            var field = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var calls = field.GetValue(buttonEvent);
                var runtimeCallsField = calls.GetType().GetField("m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (runtimeCallsField != null)
                {
                    var runtimeCalls = runtimeCallsField.GetValue(calls) as System.Collections.IList;
                    return runtimeCalls != null && runtimeCalls.Count > 0;
                }
            }
        }
        catch
        {
            // Fallback: assume no runtime listeners if reflection fails
        }
        return false;
    }

    private List<ButtonParameterInfo> GetDetailedRuntimeListenerInfo(Button.ButtonClickedEvent buttonEvent)
    {
        var result = new List<ButtonParameterInfo>();
        
        try
        {
            // Use reflection to get runtime listener information
            var field = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var calls = field.GetValue(buttonEvent);
                var runtimeCallsField = calls.GetType().GetField("m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (runtimeCallsField != null)
                {
                    var runtimeCalls = runtimeCallsField.GetValue(calls) as System.Collections.IList;
                    if (runtimeCalls != null && runtimeCalls.Count > 0)
                    {
                        for (int i = 0; i < runtimeCalls.Count; i++)
                        {
                            var call = runtimeCalls[i];
                            
                            // Get target info
                            var targetField = call.GetType().GetField("Delegate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (targetField == null)
                                targetField = call.GetType().GetField("m_Delegate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            
                            if (targetField != null)
                            {
                                var delegateObj = targetField.GetValue(call) as System.Delegate;
                                if (delegateObj != null)
                                {
                                    string targetName = "Unknown";
                                    string targetPath = "Unknown";
                                    string componentType = "Unknown";
                                    string methodName = delegateObj.Method.Name;
                                    
                                    if (delegateObj.Target != null)
                                    {
                                        if (delegateObj.Target is UnityEngine.Object unityObj)
                                        {
                                            targetName = unityObj.name;
                                            componentType = unityObj.GetType().Name;
                                            
                                            // Try to get GameObject path
                                            if (unityObj is Component comp)
                                            {
                                                targetPath = GetGameObjectPath(comp.transform);
                                            }
                                            else if (unityObj is GameObject go)
                                            {
                                                targetPath = GetGameObjectPath(go.transform);
                                            }
                                        }
                                        else
                                        {
                                            targetName = delegateObj.Target.GetType().Name;
                                            componentType = delegateObj.Target.GetType().Name;
                                            targetPath = "Non-Unity Object";
                                        }
                                    }
                                    else
                                    {
                                        targetName = "Static";
                                        componentType = delegateObj.Method.DeclaringType?.Name ?? "Unknown";
                                        targetPath = "Static Method";
                                    }
                                    
                                    // Get parameter information
                                    var parameters = delegateObj.Method.GetParameters();
                                    string paramTypes = parameters.Length > 0 ? 
                                        string.Join(", ", parameters.Select(p => p.ParameterType.Name)) : "Void";
                                    
                                    var paramInfo = new ButtonParameterInfo
                                    {
                                        parameterType = "Runtime",
                                        parameterDescription = $"Runtime Listener {i + 1}",
                                        targetGameObjectName = targetName,
                                        targetGameObjectPath = targetPath,
                                        targetComponentType = componentType,
                                        methodSignature = $"{methodName}({paramTypes})",
                                        parameterValue = $"Method: {methodName}, Parameters: ({paramTypes})",
                                        listenerIndex = i
                                    };
                                    
                                    result.Add(paramInfo);
                                    
                                    // Add parameter details if any
                                    if (parameters.Length > 0)
                                    {
                                        for (int p = 0; p < parameters.Length; p++)
                                        {
                                            var param = parameters[p];
                                            var paramDetailInfo = new ButtonParameterInfo
                                            {
                                                parameterType = "Runtime",
                                                parameterDescription = $"Parameter {p + 1}",
                                                targetGameObjectName = targetName,
                                                targetGameObjectPath = targetPath,
                                                targetComponentType = componentType,
                                                methodSignature = methodName,
                                                parameterValue = $"{param.ParameterType.Name} {param.Name}",
                                                listenerIndex = i
                                            };
                                            result.Add(paramDetailInfo);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            result.Add(new ButtonParameterInfo
            {
                parameterType = "Runtime",
                parameterDescription = "Runtime analysis error",
                parameterValue = ex.Message,
                listenerIndex = -1
            });
        }
        
        return result;
    }

    private ButtonParameterInfo GetDetailedPersistentParameterInfo(Button button, int index)
    {
        try
        {
            UnityEngine.Object target = button.onClick.GetPersistentTarget(index);
            string methodName = button.onClick.GetPersistentMethodName(index);
            
            string targetName = target != null ? target.name : "Unknown";
            string targetPath = "Unknown";
            string componentType = "Unknown";
            
            if (target != null)
            {
                componentType = target.GetType().Name;
                
                if (target is Component comp)
                {
                    targetPath = GetGameObjectPath(comp.transform);
                }
                else if (target is GameObject go)
                {
                    targetPath = GetGameObjectPath(go.transform);
                }
                else if (target is ScriptableObject)
                {
                    targetPath = "ScriptableObject Asset";
                }
            }
            
            SerializedObject serializedButton = new SerializedObject(button);
            SerializedProperty onClickProperty = serializedButton.FindProperty("m_OnClick");
            SerializedProperty callsProperty = onClickProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            string parameterValue = "Void";
            
            if (callsProperty != null && index < callsProperty.arraySize)
            {
                SerializedProperty callProperty = callsProperty.GetArrayElementAtIndex(index);
                SerializedProperty modeProperty = callProperty.FindPropertyRelative("m_Mode");
                
                if (modeProperty != null)
                {
                    PersistentListenerMode mode = (PersistentListenerMode)modeProperty.intValue;
                    
                    switch (mode)
                    {
                        case PersistentListenerMode.String:
                            var stringProp = callProperty.FindPropertyRelative("m_Arguments.m_StringArgument");
                            parameterValue = $"String: '{(stringProp != null ? stringProp.stringValue : "")}'";
                            break;
                        case PersistentListenerMode.Int:
                            var intProp = callProperty.FindPropertyRelative("m_Arguments.m_IntArgument");
                            parameterValue = $"Int: {(intProp != null ? intProp.intValue : 0)}";
                            break;
                        case PersistentListenerMode.Float:
                            var floatProp = callProperty.FindPropertyRelative("m_Arguments.m_FloatArgument");
                            parameterValue = $"Float: {(floatProp != null ? floatProp.floatValue : 0f)}";
                            break;
                        case PersistentListenerMode.Bool:
                            var boolProp = callProperty.FindPropertyRelative("m_Arguments.m_BoolArgument");
                            parameterValue = $"Bool: {(boolProp != null ? boolProp.boolValue : false)}";
                            break;
                        case PersistentListenerMode.Object:
                            var objRef = button.onClick.GetPersistentTarget(index);
                            if (objRef != null)
                            {
                                string objName = objRef.name;
                                string objType = objRef.GetType().Name;
                                parameterValue = $"Object: {objName} ({objType})";
                            }
                            else
                            {
                                parameterValue = "Object: null";
                            }
                            break;
                        default:
                            parameterValue = "Void";
                            break;
                    }
                }
            }
            
            serializedButton.Dispose();
            
            return new ButtonParameterInfo
            {
                parameterType = "Persistent",
                parameterDescription = $"Persistent Listener {index + 1}",
                targetGameObjectName = targetName,
                targetGameObjectPath = targetPath,
                targetComponentType = componentType,
                methodSignature = methodName,
                parameterValue = parameterValue,
                listenerIndex = index
            };
        }
        catch (System.Exception ex)
        {
            return new ButtonParameterInfo
            {
                parameterType = "Persistent",
                parameterDescription = "Parameter analysis error",
                parameterValue = ex.Message,
                listenerIndex = index
            };
        }
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
                var rootObjectsWithSameName = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(go => go.transform.parent == null && go.name == current.name)
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

    private void ExportToUIManager()
    {
        if (mappingData == null || mappingData.buttonMappings.Count == 0)
        {
            EditorUtility.DisplayDialog("No Data", "Please scan the scene first before exporting.", "OK");
            return;
        }

        var buttonsWithListeners = mappingData.buttonMappings.Where(m => m.hasListener).ToList();
        
        if (buttonsWithListeners.Count == 0)
        {
            EditorUtility.DisplayDialog("No Listeners", "No buttons with listeners found to export.", "OK");
            return;
        }

        string exportData = GenerateUIManagerCode(buttonsWithListeners);
        
        // Save to a text file for review
        string filePath = EditorUtility.SaveFilePanel("Save UIManager Button Methods", Application.dataPath, "UIManager_ButtonMethods", "txt");
        if (!string.IsNullOrEmpty(filePath))
        {
            System.IO.File.WriteAllText(filePath, exportData);
            Debug.Log($"UIManager button methods exported to: {filePath}");
            EditorUtility.DisplayDialog("Export Complete", $"UIManager button methods exported to:\n{filePath}", "OK");
        }
    }

    private string GenerateUIManagerCode(List<ButtonOnClickMapping> buttonsWithListeners)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("// Generated Button OnClick Methods for UIManager");
        sb.AppendLine("// Add these methods to your UIManager class");
        sb.AppendLine("// Generated on: " + DateTime.Now.ToString());
        sb.AppendLine("// Total buttons with listeners: " + buttonsWithListeners.Count);
        sb.AppendLine();
        
        // Separate buttons by listener type
        var persistentButtons = buttonsWithListeners.Where(b => b.parameters.Any(p => p.parameterType == "Persistent")).ToList();
        var runtimeButtons = buttonsWithListeners.Where(b => b.parameters.Any(p => p.parameterType == "Runtime")).ToList();
        var mixedButtons = buttonsWithListeners.Where(b => 
            b.parameters.Any(p => p.parameterType == "Persistent") && 
            b.parameters.Any(p => p.parameterType == "Runtime")).ToList();
        
        sb.AppendLine($"// Analysis Summary:");
        sb.AppendLine($"//   Persistent-only buttons: {persistentButtons.Count - mixedButtons.Count}");
        sb.AppendLine($"//   Runtime-only buttons: {runtimeButtons.Count - mixedButtons.Count}");
        sb.AppendLine($"//   Mixed (both types) buttons: {mixedButtons.Count}");
        sb.AppendLine();
        
        sb.AppendLine("// Button OnClick Event Handlers");
        sb.AppendLine("#region Button OnClick Handlers");
        sb.AppendLine();
        
        foreach (var mapping in buttonsWithListeners)
        {
            string methodName = GenerateMethodName(mapping.buttonName);
            sb.AppendLine($"    public void {methodName}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        // Button: {mapping.buttonName}");
            sb.AppendLine($"        // Path: {mapping.gameObjectPath}");
            sb.AppendLine($"        // Type: {mapping.buttonType}");
            sb.AppendLine($"        // Primary Target: {mapping.targetObjectName}");
            sb.AppendLine($"        // Primary Method: {mapping.methodName}");
            sb.AppendLine();
            
            if (mapping.parameters.Count > 0)
            {
                sb.AppendLine("        // Original Event Configuration:");
                
                var persistentParams = mapping.parameters.Where(p => p.parameterType == "Persistent").ToList();
                var runtimeParams = mapping.parameters.Where(p => p.parameterType == "Runtime").ToList();
                
                if (persistentParams.Any())
                {
                    sb.AppendLine("        // [PERSISTENT LISTENERS]");
                    foreach (var param in persistentParams)
                    {
                        sb.AppendLine($"        //   {param.parameterDescription}:");
                        sb.AppendLine($"        //     Target: {param.targetGameObjectName} ({param.targetComponentType})");
                        sb.AppendLine($"        //     Path: {param.targetGameObjectPath}");
                        sb.AppendLine($"        //     Method: {param.methodSignature}");
                        sb.AppendLine($"        //     Value: {param.parameterValue}");
                    }
                }
                
                if (runtimeParams.Any())
                {
                    sb.AppendLine("        // [RUNTIME LISTENERS]");
                    foreach (var param in runtimeParams)
                    {
                        sb.AppendLine($"        //   {param.parameterDescription}:");
                        sb.AppendLine($"        //     Target: {param.targetGameObjectName} ({param.targetComponentType})");
                        sb.AppendLine($"        //     Path: {param.targetGameObjectPath}");
                        sb.AppendLine($"        //     Method: {param.methodSignature}");
                        if (!string.IsNullOrEmpty(param.parameterValue))
                            sb.AppendLine($"        //     Details: {param.parameterValue}");
                    }
                }
                
                sb.AppendLine();
            }
            
            // Generate implementation hints based on listener types
            var hasRuntimeListeners = mapping.parameters.Any(p => p.parameterType == "Runtime");
            var hasPersistentListeners = mapping.parameters.Any(p => p.parameterType == "Persistent");
            
            if (hasRuntimeListeners)
            {
                sb.AppendLine("        // NOTE: This button had runtime listeners - check the original scripts for dynamic behavior");
                var runtimeTargets = mapping.parameters.Where(p => p.parameterType == "Runtime").Select(p => p.targetComponentType).Distinct();
                sb.AppendLine($"        // Runtime targets: {string.Join(", ", runtimeTargets)}");
            }
            
            if (hasPersistentListeners)
            {
                sb.AppendLine("        // NOTE: This button had Inspector-assigned listeners");
                var persistentValues = mapping.parameters.Where(p => p.parameterType == "Persistent" && !string.IsNullOrEmpty(p.parameterValue) && p.parameterValue != "Void").ToList();
                if (persistentValues.Any())
                {
                    sb.AppendLine("        // Original parameter values to consider:");
                    foreach (var param in persistentValues)
                    {
                        sb.AppendLine($"        //   {param.targetComponentType}.{param.methodSignature}: {param.parameterValue}");
                    }
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("        // TODO: Implement the centralized button logic here");
            sb.AppendLine("        // Replace the original functionality from the mapped target/method");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        
        sb.AppendLine("#endregion");
        sb.AppendLine();
        
        // Generate button assignment code for Start() method
        sb.AppendLine("// Add this code to your Start() method or a dedicated SetupButtonListeners() method");
        sb.AppendLine("// Button Listener Setup");
        sb.AppendLine("private void SetupButtonListeners()");
        sb.AppendLine("{");
        sb.AppendLine("    // Clear any existing listeners first");
        
        foreach (var mapping in buttonsWithListeners)
        {
            string buttonFieldName = GenerateButtonFieldName(mapping.buttonName);
            string methodName = GenerateMethodName(mapping.buttonName);
            sb.AppendLine($"    if ({buttonFieldName} != null)");
            sb.AppendLine("    {");
            sb.AppendLine($"        {buttonFieldName}.onClick.RemoveAllListeners();");
            sb.AppendLine($"        {buttonFieldName}.onClick.AddListener({methodName});");
            sb.AppendLine("    }");
        }
        
        sb.AppendLine("}");
        sb.AppendLine();
        
        // Generate button field declarations
        sb.AppendLine("// Add these button field declarations if they don't exist");
        sb.AppendLine("// Button Field Declarations");
        sb.AppendLine("[Header(\"Button References\")]");
        var uniqueButtons = buttonsWithListeners.Select(b => b.buttonName).Distinct().OrderBy(name => name);
        foreach (var buttonName in uniqueButtons)
        {
            string fieldName = GenerateButtonFieldName(buttonName);
            sb.AppendLine($"[SerializeField] private Button {fieldName};");
        }
        sb.AppendLine();
        
        // Migration instructions
        sb.AppendLine("// MIGRATION INSTRUCTIONS:");
        sb.AppendLine("// 1. Add the button field declarations to your UIManager class");
        sb.AppendLine("// 2. Assign the button references in the Inspector");
        sb.AppendLine("// 3. Call SetupButtonListeners() in your Start() method");
        sb.AppendLine("// 4. Implement the TODO sections in each button handler method");
        sb.AppendLine("// 5. Remove the original button listener assignments from other scripts");
        sb.AppendLine("// 6. Test each button to ensure functionality is preserved");
        
        return sb.ToString();
    }

    private string GenerateMethodName(string buttonName)
    {
        // Convert button name to method name (PascalCase)
        string methodName = buttonName.Replace(" ", "").Replace("_", "").Replace("-", "");
        methodName = "On" + char.ToUpper(methodName[0]) + methodName.Substring(1) + "Click";
        return methodName;
    }

    private string GenerateButtonFieldName(string buttonName)
    {
        // Convert button name to field name (camelCase)
        string fieldName = buttonName.Replace(" ", "").Replace("_", "").Replace("-", "");
        fieldName = char.ToLower(fieldName[0]) + fieldName.Substring(1);
        if (!fieldName.EndsWith("Btn") && !fieldName.EndsWith("Button"))
            fieldName += "Btn";
        return fieldName;
    }
}