#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(UIPanelEnabler))]
public class UIPanelEnablerEditor : Editor
{
    private string newPanelName = "";

    public override void OnInspectorGUI()
    {
        // Draw the default inspector (all serialized fields)
        DrawDefaultInspector();

        EditorGUILayout.Space(20);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Dynamic Panel Type Creator", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("Type a name and click 'Add' to automatically update the PanelType enum file.", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        newPanelName = EditorGUILayout.TextField("New Type Name:", newPanelName);
        
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            if (IsValidIdentifier(newPanelName))
            {
                AddNewPanelType(newPanelName);
                newPanelName = "";
                GUI.FocusControl(null); // Unfocus text field
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid C# identifier name.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void AddNewPanelType(string name)
    {
        string path = Application.dataPath + "/Scripts/PanelType.cs";
        
        if (!File.Exists(path))
        {
            Debug.LogError($"[UIPanelEnablerEditor] PanelType.cs not found at {path}");
            return;
        }

        string content = File.ReadAllText(path);
        
        // Basic check to see if it already exists
        if (content.Contains($"    {name},") || content.Contains($"    {name}\n"))
        {
            EditorUtility.DisplayDialog("Exists", $"PanelType '{name}' already exists in the enum.", "OK");
            return;
        }

        // Find the last closing brace
        int lastBraceIndex = content.LastIndexOf('}');
        if (lastBraceIndex != -1)
        {
            // Prepare the new entry
            // We check if the last entry has a comma
            string insertString = $"    {name},\n";
            
            // If the last character before '}' isn't a comma or newline, add one for safety
            // (Simple implementation assuming standard formatting)
            
            string newContent = content.Insert(lastBraceIndex, insertString);
            
            File.WriteAllText(path, newContent);
            AssetDatabase.Refresh();
            
            Debug.Log($"[UIPanelEnablerEditor] Successfully added '{name}' to PanelType enum.");
        }
        else
        {
            Debug.LogError("[UIPanelEnablerEditor] Could not find closing brace in PanelType.cs");
        }
    }

    private bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_') return false;
        }
        return true;
    }
}
#endif
