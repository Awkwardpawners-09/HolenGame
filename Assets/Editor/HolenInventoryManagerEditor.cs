using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(HolenInventoryManager))]
public class HolenInventoryManagerEditor : Editor
{
    private HolenInventoryManager manager;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        manager = (HolenInventoryManager)target;

        // Show Holen database list
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allHolens"), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory Entries", EditorStyles.boldLabel);

        SerializedProperty inventoryList = serializedObject.FindProperty("inventory");

        for (int i = 0; i < inventoryList.arraySize; i++)
        {
            SerializedProperty entry = inventoryList.GetArrayElementAtIndex(i);
            SerializedProperty idProp = entry.FindPropertyRelative("holenID");
            SerializedProperty qtyProp = entry.FindPropertyRelative("quantity");

            EditorGUILayout.BeginHorizontal();

            // Dropdown for Holen ID (from allHolens)
            List<string> options = manager.allHolens.ConvertAll(h => h.holenID);
            int selectedIndex = Mathf.Max(0, options.IndexOf(idProp.stringValue));
            selectedIndex = EditorGUILayout.Popup(selectedIndex, options.ToArray());

            if (selectedIndex >= 0 && selectedIndex < options.Count)
            {
                idProp.stringValue = options[selectedIndex];
            }

            // Quantity field
            qtyProp.intValue = EditorGUILayout.IntField(qtyProp.intValue, GUILayout.MaxWidth(60));

            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                inventoryList.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // Add new entry button
        if (GUILayout.Button("Add Entry"))
        {
            inventoryList.arraySize++;
            var newEntry = inventoryList.GetArrayElementAtIndex(inventoryList.arraySize - 1);
            newEntry.FindPropertyRelative("holenID").stringValue = manager.allHolens.Count > 0 ? manager.allHolens[0].holenID : "";
            newEntry.FindPropertyRelative("quantity").intValue = 1;
        }

        serializedObject.ApplyModifiedProperties();
    }
}