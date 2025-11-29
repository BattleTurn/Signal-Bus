using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using BattleTurn.SignalBus;

[CustomPropertyDrawer(typeof(SignalBus))]
public class SignalBusDrawer : PropertyDrawer
{
    private bool _foldout;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        var bus = fieldInfo.GetValue(property.serializedObject.targetObject) as SignalBus;
        if (bus == null) return line + 4f;
        if (!_foldout) return line + 4f;

        int pipeCount = bus.DebugGetPipes().Count;
        // + extra vertical spacing after foldout
        return (1 + pipeCount) * line + EditorGUIUtility.standardVerticalSpacing - 4f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var bus = fieldInfo.GetValue(property.serializedObject.targetObject) as SignalBus;
        if (bus == null)
        {
            EditorGUI.LabelField(position, label.text, "null");
            EditorGUI.EndProperty();
            return;
        }

        _foldout = EditorGUI.Foldout(position, _foldout, label, true);
        if (!_foldout)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        var pipes = bus.DebugGetPipes();
        float startY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // thêm khoảng cách
        var lineRect = new Rect(position.x, startY, position.width, EditorGUIUtility.singleLineHeight);
        foreach (var kv in pipes)
        {
            DrawPipeLine(lineRect, kv.Key, kv.Value);
            lineRect.y += lineRect.height;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private void DrawPipeLine(Rect rect, Type type, object pipeObj)
    {
        // Get listener counts via reflection (properties or fallback fields)
        int param = GetInt(pipeObj, "ParamListenerCount", "_countParam");
        int noArg = GetInt(pipeObj, "NoArgListenerCount", "_countNoArg");
        string label = $"{type.Name}: Action<T>={param}, Action={noArg}";
        EditorGUI.LabelField(rect, label);
    }

    private static int GetInt(object instance, string propName, string fieldName)
    {
        if (instance == null) return -1;
        var t = instance.GetType();
        var p = t.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(int))
        {
            var v = p.GetValue(instance);
            if (v is int i) return i;
        }
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(int))
        {
            var v = f.GetValue(instance);
            if (v is int i) return i;
        }
        return -1;
    }
}
