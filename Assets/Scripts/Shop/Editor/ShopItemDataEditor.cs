using UnityEngine;
using UnityEditor;
using System.Linq;
using Base;

/// <summary>
/// Custom editor for ShopItemData
/// Provides dropdown for item selection from DefaultSettings.ShopItems
/// </summary>
[CustomEditor(typeof(ShopItemData))]
public class ShopItemDataEditor : Editor
{
    private SerializedProperty itemIdProp;
    private SerializedProperty iconProp;
    private SerializedProperty isAvailableProp;
    
    private string[] itemIds;
    private int selectedIndex = 0;
    
    private void OnEnable()
    {
        itemIdProp = serializedObject.FindProperty("itemId");
        iconProp = serializedObject.FindProperty("icon");
        isAvailableProp = serializedObject.FindProperty("isAvailable");
        
        // Get all item IDs from DefaultSettings
        itemIds = DefaultSettings.ShopItems.Keys.OrderBy(k => k).ToArray();
        
        // Find current selection
        string currentId = itemIdProp.stringValue;
        if (!string.IsNullOrEmpty(currentId))
        {
            selectedIndex = System.Array.IndexOf(itemIds, currentId);
            if (selectedIndex < 0) selectedIndex = 0;
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        ShopItemData itemData = (ShopItemData)target;
        
        EditorGUILayout.LabelField("Item Selection", EditorStyles.boldLabel);
        
        // Item ID dropdown
        EditorGUI.BeginChangeCheck();
        selectedIndex = EditorGUILayout.Popup("Item ID", selectedIndex, itemIds);
        if (EditorGUI.EndChangeCheck())
        {
            itemIdProp.stringValue = itemIds[selectedIndex];
            
            // Auto-generate asset name
            string newName = $"Item_{itemIds[selectedIndex]}";
            string assetPath = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.RenameAsset(assetPath, newName);
            }
            
            EditorUtility.SetDirty(target);
        }
        
        EditorGUILayout.Space();
        
        // Show item info from DefaultSettings
        if (!string.IsNullOrEmpty(itemIdProp.stringValue))
        {
            if (DefaultSettings.ShopItems.TryGetValue(itemIdProp.stringValue, out var info))
            {
                EditorGUILayout.LabelField("Item Info (from DefaultSettings)", EditorStyles.boldLabel);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Category", info.Category.ToString());
                EditorGUILayout.TextField("Type", info.Type.ToString());
                EditorGUILayout.IntField("Effect Value", info.EffectValue);
                EditorGUILayout.IntField("Price", info.Price);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
                
                // Show localization keys
                EditorGUILayout.LabelField("Localization Keys", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Name Key", $"Item_{itemIdProp.stringValue}_Name");
                EditorGUILayout.TextField("Description Key", $"Item_{itemIdProp.stringValue}_Desc");
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox($"Item ID '{itemIdProp.stringValue}' not found in DefaultSettings.ShopItems!", MessageType.Error);
            }
        }
        
        // Visual settings
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(iconProp);
        EditorGUILayout.PropertyField(isAvailableProp);
        
        serializedObject.ApplyModifiedProperties();
    }
}
