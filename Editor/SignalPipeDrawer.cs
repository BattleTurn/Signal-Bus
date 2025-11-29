using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BattleTurn.SignalBus.Editor
{
    /// <summary>
    /// Draws SignalPipe&lt;T&gt; with listener counts. Works for nested fields and list elements.
    /// </summary>
    [CustomPropertyDrawer(typeof(SignalPipe<>))]
    internal sealed class SignalPipeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 1 title line + 2 info lines + small padding
            return EditorGUIUtility.singleLineHeight * 3f + 6f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Giữ label + indent chuẩn
            var labelRect = new Rect(line.x, line.y, EditorGUIUtility.labelWidth, line.height);
            var valueRect = new Rect(line.x + EditorGUIUtility.labelWidth, line.y, line.width - EditorGUIUtility.labelWidth, line.height);
            EditorGUI.PrefixLabel(labelRect, label);
            // hiển thị type name hoặc gì đó nhẹ ở valueRect nếu muốn
            line.y += line.height;

            int paramCount = -1;
            int noArgCount = -1;
            string sourceNote = "";

            object instance = TryGetTargetInstance(property);
            if (instance != null)
            {
                paramCount = GetIntProp(instance, "ParamListenerCount", fallbackField: "_countParam");
                noArgCount = GetIntProp(instance, "NoArgListenerCount", fallbackField: "_countNoArg");
                sourceNote = "instance";
            }

            if (paramCount >= 0 && noArgCount >= 0)
            {
                EditorGUI.LabelField(line, $"Action<T> listeners: {paramCount}");
                line.y += line.height;
                EditorGUI.LabelField(line, $"Action listeners: {noArgCount}  {(string.IsNullOrEmpty(sourceNote) ? "" : $"({sourceNote})")}");
                line.y += line.height;
            }
            else
            {
                EditorGUI.HelpBox(new Rect(line.x, line.y, line.width, EditorGUIUtility.singleLineHeight * 2f),
                    "SignalPipe instance not found. Ensure the field is initialized (e.g., = new()) or use [SerializeReference].",
                    MessageType.Info);
            }

            EditorGUI.EndProperty();
        }

        // Resolve the actual managed object represented by this SerializedProperty,
        // handling nested fields and arrays/lists (Array.data[x]).
        private static object TryGetTargetInstance(SerializedProperty property)
        {
            try
            {
                object obj = property.serializedObject.targetObject;
                var path = property.propertyPath.Replace(".Array.data[", "[");
                var elements = path.Split('.');

                foreach (var element in elements)
                {
                    if (element.Contains("["))
                    {
                        // It's an indexed element: fieldName[index]
                        var fieldName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                        var indexStr = element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Trim('[', ']');
                        int index = int.Parse(indexStr);

                        obj = GetFieldValue(obj, fieldName);
                        if (obj is IList list)
                        {
                            if (index < 0 || index >= list.Count) return null;
                            obj = list[index];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        obj = GetFieldValue(obj, element);
                    }

                    if (obj == null) return null;
                }

                return obj;
            }
            catch
            {
                return null;
            }
        }

        private static object GetFieldValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();

            // Try field
            var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f.GetValue(source);

            // Try property
            var p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) return p.GetValue(source);

            return null;
        }

        // Read an int from a property first, then fallback to a field. Returns -1 if missing.
        private static int GetIntProp(object source, string propertyName, string fallbackField)
        {
            if (source == null) return -1;
            var type = source.GetType();

            // Try property
            var p = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(int))
            {
                var val = p.GetValue(source);
                if (val is int i) return i;
            }

            // Fallback to field
            var f = type.GetField(fallbackField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                var val = f.GetValue(source);
                if (val is int i) return i;
            }

            return -1;
        }
    }
}