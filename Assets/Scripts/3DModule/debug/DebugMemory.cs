

/**
 * \file    DebugMemory.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define DebugMemory class and it's associated editor
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif



namespace HBP.VISU3D
{

#if UNITY_EDITOR
    /// <summary>
    /// 
    /// </summary>
    [CustomEditor(typeof(DebugMemory))]
    public class DebugMemory_Editor : Editor
    {
        SerializedProperty TimeCheck;

        SerializedProperty TotalCount;
        SerializedProperty GOCount;
        SerializedProperty MeshCount;
        SerializedProperty TextureCount;
        SerializedProperty MaterialCount;
        SerializedProperty ComponentCount;

        void OnEnable()
        {
            TotalCount = serializedObject.FindProperty("m_totalCount");
            GOCount = serializedObject.FindProperty("m_GOCount");
            MeshCount = serializedObject.FindProperty("m_meshCount");
            TextureCount = serializedObject.FindProperty("m_textureCount");
            MaterialCount = serializedObject.FindProperty("m_materialCount");
            ComponentCount = serializedObject.FindProperty("m_componentCount");
            TimeCheck = serializedObject.FindProperty("m_timeCheck");            
        }

        public override void OnInspectorGUI()
        {
            //DebugMemory myTarget = (DebugMemory)target;
            //EditorGUILayout.LabelField("Refresh (s) :       " + TimeCheck.floatValue);
            EditorGUILayout.PropertyField(TimeCheck, new GUIContent("Refresh (s) "));
            EditorGUILayout.LabelField("Total count            " + TotalCount.intValue);
            EditorGUILayout.LabelField("GO count               " + GOCount.intValue);
            EditorGUILayout.LabelField("Mesh  count            " + MeshCount.intValue);
            EditorGUILayout.LabelField("Texture count          " + TextureCount.intValue);
            EditorGUILayout.LabelField("Material count         " + MaterialCount.intValue);
            EditorGUILayout.LabelField("Component count        " + ComponentCount.intValue);
            //EditorGUILayout.PropertyField(TotalCount, new GUIContent("Total count "));
            //EditorGUILayout.PropertyField(GOCount, new GUIContent("GO count "));
            //EditorGUILayout.PropertyField(MeshCount, new GUIContent("Mesh count "));
            //EditorGUILayout.PropertyField(TextureCount, new GUIContent("Texture count "));
            //EditorGUILayout.PropertyField(MaterialCount, new GUIContent("Material count "));
            //EditorGUILayout.PropertyField(ComponentCount, new GUIContent("Component count "));

            if (GUILayout.Button("Call GC", new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(50) }))
            {
                DebugMemory module = (DebugMemory)target;
                module.callGC();
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif

    public class DebugMemory : MonoBehaviour
    {
        public int m_totalCount;
        public int m_GOCount;
        public int m_meshCount;
        public int m_textureCount;
        public int m_materialCount;
        public int m_componentCount;


        float m_deltaTime = 0.0f;
        float m_timeElapsed = 0f;

        public float m_timeCheck = 1; // maximum interval time (s) between two memory checks

        public void callGC()
        {
            GC.Collect();
        }

        // Update is called once per frame
        void Update()
        {
            m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;
            m_timeElapsed += m_deltaTime;

            // limit the memory check to timeCheck
            if (m_timeElapsed > m_timeCheck)
            {
                // reset elapsed time
                m_timeElapsed = 0f;

                // check memory            
                m_totalCount = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)).Length;
                m_GOCount = Resources.FindObjectsOfTypeAll(typeof(GameObject)).Length;
                m_meshCount = Resources.FindObjectsOfTypeAll(typeof(Mesh)).Length;
                m_textureCount = Resources.FindObjectsOfTypeAll(typeof(Texture)).Length;
                m_materialCount = Resources.FindObjectsOfTypeAll(typeof(Material)).Length;
                m_componentCount = Resources.FindObjectsOfTypeAll(typeof(Component)).Length;
            }
        }
    }
}