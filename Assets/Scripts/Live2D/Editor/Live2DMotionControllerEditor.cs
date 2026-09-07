using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace MaisLoveStory.Live2D.Editor
{
    [CustomEditor(typeof(Live2DMotionController))]
    public class Live2DMotionControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty animatorProp;
        
        private List<string> availableParameters = new List<string>();
        private string defaultStateName = "";
        
        private void OnEnable()
        {
            animatorProp = serializedObject.FindProperty("animator");
            
            RefreshAnimatorInfo();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live2D Motion Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Automatically reads animator parameters and default state. Loop values come from dialogue data.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Animator field
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(animatorProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                RefreshAnimatorInfo();
            }
            
            // Show refresh button
            if (GUILayout.Button("Refresh Animator Info"))
            {
                RefreshAnimatorInfo();
            }
            
            EditorGUILayout.Space();
            
            // Show default state
            if (!string.IsNullOrEmpty(defaultStateName))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Default State:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"→ {defaultStateName}", EditorStyles.largeLabel);
                EditorGUILayout.HelpBox("This is the animator's default state. Character will return to this state when idle.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
            
            // Show available parameters
            if (availableParameters.Count > 0)
            {
                EditorGUILayout.LabelField("Available Parameters:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                Animator animator = animatorProp.objectReferenceValue as Animator;
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    var parameters = animator.parameters;
                    foreach (var param in parameters)
                    {
                        string paramInfo = $"• {param.name} ({param.type})";
                        if (param.type == AnimatorControllerParameterType.Float)
                        {
                            paramInfo += $" [Default: {param.defaultFloat}]";
                        }
                        else if (param.type == AnimatorControllerParameterType.Int)
                        {
                            paramInfo += $" [Default: {param.defaultInt}]";
                        }
                        else if (param.type == AnimatorControllerParameterType.Bool)
                        {
                            paramInfo += $" [Default: {param.defaultBool}]";
                        }
                        EditorGUILayout.LabelField(paramInfo);
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No animator assigned or no parameters found.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Test buttons in play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Test Motions (Play Mode)", EditorStyles.boldLabel);
                
                Live2DMotionController controller = target as Live2DMotionController;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (GUILayout.Button($"Play Default Motion ({defaultStateName})"))
                {
                    controller.PlayDefaultMotion();
                }
                
                EditorGUILayout.Space();
                
                if (availableParameters.Count > 0)
                {
                    EditorGUILayout.LabelField("Play Specific Motion:");
                    
                    // Create a grid of buttons
                    int buttonsPerRow = 3;
                    for (int i = 0; i < availableParameters.Count; i += buttonsPerRow)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int j = 0; j < buttonsPerRow && (i + j) < availableParameters.Count; j++)
                        {
                            string paramName = availableParameters[i + j];
                            if (GUILayout.Button(paramName))
                            {
                                controller.PlayMotion(paramName, loop: true, floatValue: 0.5f);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void RefreshAnimatorInfo()
        {
            availableParameters.Clear();
            defaultStateName = "";
            
            Animator animator = animatorProp.objectReferenceValue as Animator;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                // Get default state
                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller != null && controller.layers.Length > 0)
                {
                    var defaultState = controller.layers[0].stateMachine.defaultState;
                    if (defaultState != null)
                    {
                        defaultStateName = defaultState.name;
                    }
                }
                
                // Get parameters
                var parameters = animator.parameters;
                foreach (var param in parameters)
                {
                    availableParameters.Add(param.name);
                }
            }
        }
    }
}
