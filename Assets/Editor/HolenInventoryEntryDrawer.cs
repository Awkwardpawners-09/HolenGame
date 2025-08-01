using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(HolenInventoryEntry))]
public class HolenInventoryEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var holenIDProp = property.FindPropertyRelative("holenID");
        var quantityProp = property.FindPropertyRelative("quantity");

        EditorGUI.BeginProperty(position, label, property);

        // Get all HolenData assets in project
        string[] guids = AssetDatabase.FindAssets("t:HolenData");
        List<string> names = new List<string>();
        foreach (string guid in guids)
        {
            var data = AssetDatabase.LoadAssetAtPath<HolenData>(AssetDatabase.GUIDToAssetPath(guid));
            names.Add(data.holenID);
        }

        // Create dropdown
        int selectedIndex = Mathf.Max(0, names.IndexOf(holenIDProp.stringValue));
        selectedIndex = EditorGUI.Popup(
            new Rect(position.x, position.y, position.width * 0.6f, EditorGUIUtility.singleLineHeight),
            "Holen",
            selectedIndex,
            names.ToArray()
        );

        // Assign holenID based on selection
        if (selectedIndex >= 0 && selectedIndex < names.Count)
        {
            holenIDProp.stringValue = names[selectedIndex];
        }

        // Quantity field
        EditorGUI.PropertyField(
            new Rect(position.x + position.width * 0.65f, position.y, position.width * 0.3f, EditorGUIUtility.singleLineHeight),
            quantityProp,
            GUIContent.none
        );

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
