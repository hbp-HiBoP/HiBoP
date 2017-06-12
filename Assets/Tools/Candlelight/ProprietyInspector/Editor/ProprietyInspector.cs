using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Text.RegularExpressions;

[CustomEditor(typeof(MonoBehaviourWithProp),true)]
public class ProprietyInspector : Editor
{
    public override void OnInspectorGUI()
    {
        PropertyInfo[] properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var property in properties)
        {
            string label = SplitCamelCase(property.Name);
            // Bool
            if (property.PropertyType == typeof(bool))
            {
                if (property.CanRead && property.CanWrite)
                {
                    string[] states = new string[] { bool.FalseString, bool.TrueString };
                    bool state = (bool)property.GetValue(target, null);
                    int i;
                    if (state) i = 1;
                    else i = 0;
                    i = EditorGUILayout.Popup(label, i, states);
                    if (i == 0) state = false;
                    else state = true;
                    property.SetValue(target, state, null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((bool)(property.GetValue(target, null))).ToString());
                }
            }

            // Byte
            else if (property.PropertyType == typeof(byte))
            {
                if (property.CanRead && property.CanWrite)
                {
                    int min = byte.MinValue;
                    int max = byte.MaxValue;
                    byte byteValue = (byte) property.GetValue(target, null);
                    byteValue = (byte)Mathf.Clamp(EditorGUILayout.IntField(label, (int)byteValue), min, max);
                    property.SetValue(target, byteValue, null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((byte)(property.GetValue(target, null))).ToString());
                }
            }

            // Char
            else if (property.PropertyType == typeof(char))
            {
                if (property.CanRead && property.CanWrite)
                {
                    char c = (char)property.GetValue(target, null);
                    string result = EditorGUILayout.TextField(label, c.ToString());
                    if(result.Length > 0)
                    {
                        property.SetValue(target,result[0], null);
                    }
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((string)(property.GetValue(target, null))));
                }
            }

            // Int
            else if (property.PropertyType == typeof(int))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target, EditorGUILayout.IntField(label, (int)property.GetValue(target, null)), null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((int)(property.GetValue(target, null))).ToString());
                }
            }

            // Float
            else if(property.PropertyType == typeof(float))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target, EditorGUILayout.FloatField(label, (float)property.GetValue(target, null)), null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((float)(property.GetValue(target, null))).ToString());
                }
            }

            // Double
            else if (property.PropertyType == typeof(double))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target, EditorGUILayout.DoubleField(label, (double)property.GetValue(target, null)), null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((double)(property.GetValue(target, null))).ToString());
                }
            }

            // String
            else if (property.PropertyType == typeof(string))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target, EditorGUILayout.TextField(label, (string)property.GetValue(target, null)), null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((string)(property.GetValue(target, null))));
                }
            }

            // Vector2
            else if (property.PropertyType == typeof(Vector2))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target, EditorGUILayout.Vector2Field(label, (Vector2)property.GetValue(target, null)), null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((Vector2)(property.GetValue(target, null))).ToString());
                }
            }

            // Vector3
            else if (property.PropertyType == typeof(Vector3))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target, EditorGUILayout.Vector3Field(label, (Vector3)property.GetValue(target, null)), null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((Vector3)(property.GetValue(target, null))).ToString());
                }
            }

            // Vector4
            else if (property.PropertyType == typeof(Vector4))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target, EditorGUILayout.Vector4Field(label, (Vector4)property.GetValue(target, null)), null);
                }
                else if (property.CanRead)
                {
                    EditorGUILayout.LabelField(label, ((Vector4)(property.GetValue(target, null))).ToString());
                }
            }

            // UnityEngine.Object
            else if(property.PropertyType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(target,EditorGUILayout.ObjectField(label, (UnityEngine.Object)property.GetValue(target, null), property.PropertyType, true),null);
                }
            }
        }   
    }

    string SplitCamelCase(string camelCase)
    {
        return Regex.Replace(camelCase, "(?=\\p{Lu}\\p{Ll})|(?<=\\p{Ll})(?=\\p{Lu})", " ", RegexOptions.Compiled).Trim();
    }
}
