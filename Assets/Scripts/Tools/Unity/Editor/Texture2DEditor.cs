#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Tools.Unity
{
    public class Texture2DEditor : EditorWindow
    {
        Texture2D m_textureToRotate;
        Texture2D m_textureRotated;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Tools/Rotate texture")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Texture2DEditor window = (Texture2DEditor)GetWindow(typeof(Texture2DEditor));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Rotate Textures", EditorStyles.boldLabel);
            m_textureToRotate = (Texture2D)EditorGUILayout.ObjectField("Texture to rotate :", m_textureToRotate, typeof(Texture2D), false);
            if (GUILayout.Button("Save"))
            {
                m_textureRotated = Texture2DExtension.RotateTexture(m_textureToRotate);
                string path = EditorUtility.SaveFilePanelInProject("Save texture as PNG", "", m_textureRotated.name + "png", "png");

                byte[] bytes = m_textureRotated.EncodeToPNG();
                DestroyImmediate(m_textureRotated);
                File.WriteAllBytes(path, bytes);
            }
        }
    }
}
#endif