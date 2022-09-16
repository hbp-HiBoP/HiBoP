using UnityEngine;
using UnityEditor;

namespace HBP.UI.Tools
{
	public class WindowMenu : EditorWindow
	{
		string m_windowTitle;
		GameObject m_content;
		RectTransform m_minimizeBarPanel;
		[MenuItem("GameObject/UI/Window %#w")]
		public static void ShowWindow()
		{
			GetWindow(typeof(WindowMenu));
		}

		void OnGUI()
		{
			if(!Selection.activeGameObject)
			{
				GUILayout.Label( "Select the parent of the window in the hierarchy view" );
			}
			else
			{
				m_windowTitle = EditorGUILayout.TextField( "Title: ", m_windowTitle );
				m_content = (GameObject) EditorGUILayout.ObjectField("Content",m_content,typeof(GameObject),false);
				m_minimizeBarPanel = (RectTransform) EditorGUILayout.ObjectField("Minimize Bar",m_minimizeBarPanel,typeof(RectTransform),true);
				if( GUILayout.Button( "Create" ) )
				{
					if( m_windowTitle != "" && m_content != null && m_minimizeBarPanel != null )
					{
						GameObject l_window = Instantiate(Resources.Load("Window", typeof(GameObject))) as GameObject;
						l_window.transform.SetParent(Selection.activeGameObject.transform);
						l_window.name = m_windowTitle;
						l_window.GetComponent<WindowGestion>().initialize(m_windowTitle,m_content,m_minimizeBarPanel);
					}
				}
			}
			if(GUILayout.Button("Close"))
            {
                Close();
                Repaint();
            }
		}
	}
}
