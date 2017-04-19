

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
            EditorGUILayout.PropertyField(TimeCheck, new GUIContent("Refresh (s) "));
            EditorGUILayout.LabelField("Total count            " + TotalCount.intValue);
            EditorGUILayout.LabelField("GO count               " + GOCount.intValue);
            EditorGUILayout.LabelField("Mesh  count            " + MeshCount.intValue);
            EditorGUILayout.LabelField("Texture count          " + TextureCount.intValue);
            EditorGUILayout.LabelField("Material count         " + MaterialCount.intValue);
            EditorGUILayout.LabelField("Component count        " + ComponentCount.intValue);

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

    /// <summary>
    /// A class for getting the number of elements allocated
    /// </summary>
    public class DebugMemory : MonoBehaviour
    {
        public int totalCount;      /**< total number of elements allocated */
        public int GOCount;         /**< number of GO allocated */
        public int meshCount;       /**< number of mesh allocated */
        public int textureCount;    /**< number of textures allocated */
        public int materialCount;   /**< number of materials allocated */
        public int componentCount;  /**< number of components allocated */

        public float timeCheck = 1; /**< maximum interval time (s) between two memory checks */

        private float m_deltaTime = 0.0f;
        private float m_timeElapsed = 0f;

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
            m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;
            m_timeElapsed += m_deltaTime;

            // limit the memory check to timeCheck
            if (m_timeElapsed > timeCheck)
            {
                // reset elapsed time
                m_timeElapsed = 0f;

                // check memory            
                totalCount = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)).Length;
                GOCount = Resources.FindObjectsOfTypeAll(typeof(GameObject)).Length;
                meshCount = Resources.FindObjectsOfTypeAll(typeof(Mesh)).Length;
                textureCount = Resources.FindObjectsOfTypeAll(typeof(Texture)).Length;
                materialCount = Resources.FindObjectsOfTypeAll(typeof(Material)).Length;
                componentCount = Resources.FindObjectsOfTypeAll(typeof(Component)).Length;
            }
        }
    }
}