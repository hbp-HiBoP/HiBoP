

/**
 * \file    DebugMemory.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define DebugMemory class and it's associated editor
 */

// system
using System;

// unity
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HBP.Module3D
{

#if UNITY_EDITOR
    /// <summary>
    /// DebugMemory custom inspector
    /// </summary>
    [CustomEditor(typeof(DebugMemory))]
    public class DebugMemory_Editor : Editor
    {
        SerializedProperty TimeCheck;

        SerializedProperty TotalCount;
        SerializedProperty GameObjectsCount;
        SerializedProperty MeshCount;
        SerializedProperty TextureCount;
        SerializedProperty MaterialCount;
        SerializedProperty ComponentCount;

        void OnEnable()
        {
            TotalCount = serializedObject.FindProperty("TotalCount");
            GameObjectsCount = serializedObject.FindProperty("GameObjectsCount");
            MeshCount = serializedObject.FindProperty("MeshCount");
            TextureCount = serializedObject.FindProperty("TextureCount");
            MaterialCount = serializedObject.FindProperty("MaterialCount");
            ComponentCount = serializedObject.FindProperty("ComponentCount");
            TimeCheck = serializedObject.FindProperty("TimeCheck");            
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(TimeCheck, new GUIContent("Refresh (s) "));
            EditorGUILayout.LabelField("Total count\t\t" + TotalCount.intValue);
            EditorGUILayout.LabelField("GameObjects count\t" + GameObjectsCount.intValue);
            EditorGUILayout.LabelField("Mesh count\t" + MeshCount.intValue);
            EditorGUILayout.LabelField("Texture count\t" + TextureCount.intValue);
            EditorGUILayout.LabelField("Material count\t" + MaterialCount.intValue);
            EditorGUILayout.LabelField("Component count\t" + ComponentCount.intValue);

            if (GUILayout.Button("Call Garbage Collector", new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(30) }))
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

    /// <summary>
    /// A class for getting the number of elements allocated
    /// </summary>
    public class DebugMemory : MonoBehaviour
    {
        public int TotalCount;      /**< total number of elements allocated */
        public int GameObjectsCount;         /**< number of GO allocated */
        public int MeshCount;       /**< number of mesh allocated */
        public int TextureCount;    /**< number of textures allocated */
        public int MaterialCount;   /**< number of materials allocated */
        public int ComponentCount;  /**< number of components allocated */
        public float TimeCheck = 1; /**< maximum interval time (s) between two memory checks */

        private float m_DeltaTime = 0.0f;
        private float m_TimeElapsed = 0f;

        /// <summary>
        /// Force the calling of the garbage collector
        /// </summary>
        public void callGC()
        {
            GC.Collect();
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        void Update()
        {
            m_DeltaTime += (Time.deltaTime - m_DeltaTime) * 0.1f;
            m_TimeElapsed += m_DeltaTime;

            // limit the memory check to timeCheck
            if (m_TimeElapsed > TimeCheck)
            {
                // reset elapsed time
                m_TimeElapsed = 0f;

                // check memory            
                TotalCount = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)).Length;
                GameObjectsCount = Resources.FindObjectsOfTypeAll(typeof(GameObject)).Length;
                MeshCount = Resources.FindObjectsOfTypeAll(typeof(Mesh)).Length;
                TextureCount = Resources.FindObjectsOfTypeAll(typeof(Texture)).Length;
                MaterialCount = Resources.FindObjectsOfTypeAll(typeof(Material)).Length;
                ComponentCount = Resources.FindObjectsOfTypeAll(typeof(Component)).Length;
            }
        }
    }
}