using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class DialogueDrawer : PropertyAttribute
{
    public readonly string[] names;
    public DialogueDrawer(string[] names) { this.names = names; }
    public DialogueDrawer(Type enumType) { names = Enum.GetNames(enumType); }
}

[CustomPropertyDrawer(typeof(DialogueDrawer))]
public class DialogueArrayDrawer : PropertyDrawer
{
    // YITTER
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, label, property);
        try
        {
            var path = property.propertyPath;
            int pos = (int)System.Char.GetNumericValue(path[path.LastIndexOf('[')+1]);
            EditorGUI.PropertyField(rect, property, new GUIContent(((DialogueDrawer)attribute).names[pos]), true);
        }
        catch
        {
            EditorGUI.PropertyField(rect, property, label, true);
        }
        EditorGUI.EndProperty();
    }
}


