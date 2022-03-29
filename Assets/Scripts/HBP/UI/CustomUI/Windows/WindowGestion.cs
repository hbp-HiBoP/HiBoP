using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Window
{
	[RequireComponent (typeof (Resizer))]
	[RequireComponent (typeof (Dragger))]
	[RequireComponent (typeof (RectTransform))]
	public class WindowGestion : MonoBehaviour 
	{
		#region Attributs
		/* RectTransforms */
		private RectTransform m_rectTransform;
		private RectTransform m_parentRectTransform;

		/* Resize and drag scripts */
		private Resizer m_resizeWindow;
		private Dragger m_dragWindow;

		/* Title of the window */
		private Text m_title;

		/* Vector2 initial position + offset */
		private Vector2 m_originalAnchorMaxPosition;
		private Vector2 m_originalAnchorMinPosition;

		/* Vector2 last postion + offset */
		private Vector2 m_lastLocalPointerPosition;
		private Vector2 m_lastOffsetMin;
		private Vector2 m_lastOffsetMax;
		
		/* State windows */
		public bool Interactable{set{setInteractble(value);}}
		public bool IsActive{set{setActive(value);}}
		public enum WindowState{Maximized,Minimized,Normal}
		private WindowState m_state = WindowState.Normal;
		public WindowState State{get{return m_state;}set{setWindowState(value);}}

		/* SetWindow */
		public string Title;
		public GameObject Content;
		public RectTransform MinimizePanelBar;
		#endregion

		#region Initialisation
		void Start()
		{
			initialize();
		}
		public void initialize()
		{
			m_resizeWindow = transform.GetComponent<Resizer>();
			m_dragWindow =transform.GetComponent<Dragger>();

			initializeTitle();
			initializeRectTransform();
			initializeContent();
		}

		public void initialize(string title,GameObject content,RectTransform minimizeBar)
		{
			Title = title;
			Content = content;
			MinimizePanelBar = minimizeBar;
			m_resizeWindow = transform.GetComponent<Resizer>();
			m_dragWindow =transform.GetComponent<Dragger>();

			initializeTitle();
			initializeRectTransform();
			initializeContent();

		}

		public void setActive(bool isActive)
		{
			gameObject.SetActive(isActive);
			Interactable = isActive;
			showWindowInFirst();
		}

		private void initializeRectTransform()
		{
			m_rectTransform = transform.GetComponent<RectTransform>();
			m_parentRectTransform = transform.parent.GetComponent<RectTransform>();
			m_originalAnchorMaxPosition = m_rectTransform.anchorMax;
			m_originalAnchorMinPosition = m_rectTransform.anchorMin;
			calculateMinimumSize();
		}
		private void initializeTitle()
		{
			m_title = transform.Find("Header").Find("Title").GetComponent<Text>();
			m_title.text = Title;
		}

		private void initializeContent()
		{
			if(transform.Find("Content").childCount == 0 )
			{
				GameObject l_content = GameObject.Instantiate(Content) as GameObject;
				l_content.transform.SetParent(transform.GetChild(1));
				RectTransform l_contentRectTransform = l_content.GetComponent<RectTransform>();
				l_contentRectTransform.anchorMax = new Vector2(1,1);
				l_contentRectTransform.anchorMin = new Vector2(0,0);
				l_contentRectTransform.offsetMin = new Vector2(0,0);
				l_contentRectTransform.offsetMax = new Vector2(0,0);
				l_contentRectTransform.localPosition = new Vector3(0,0,0);
				l_contentRectTransform.localScale = new Vector3(1,1,1);
			}
		}
		#endregion

		#region OnEvent()
		public void OnMouseClick()
		{
			showWindowInFirst();
		}

		public void OnMaximizeWindow()
		{
			showWindowInFirst();
			if(State == WindowState.Normal)
			{
				m_lastOffsetMin = m_rectTransform.offsetMin;
				m_lastOffsetMax = m_rectTransform.offsetMax;
				m_rectTransform.anchorMin = new Vector2(0,0);
				m_rectTransform.anchorMax = new Vector2(1,1);
				m_rectTransform.offsetMin = new Vector2(0,0);
				m_rectTransform.offsetMax = new Vector2(0,0);
				State = WindowState.Maximized;
			}
			else if(State == WindowState.Maximized)
			{
				m_rectTransform.anchorMin = m_originalAnchorMinPosition;
				m_rectTransform.anchorMax = m_originalAnchorMaxPosition;
				m_rectTransform.offsetMin = m_lastOffsetMin;
				m_rectTransform.offsetMax = m_lastOffsetMax;
				State = WindowState.Normal;
			}
			else if(State == WindowState.Minimized)
			{
				m_rectTransform.SetParent(m_parentRectTransform);
				m_rectTransform.anchorMin = new Vector2(0,0);
				m_rectTransform.anchorMax = new Vector2(1,1);
				m_rectTransform.offsetMin = new Vector2(0,0);
				m_rectTransform.offsetMax = new Vector2(0,0);
				State = WindowState.Maximized;
			}
		}

		public void OnMinimizeWindow()
		{
			if(State == WindowState.Normal)
			{
				m_lastOffsetMin = m_rectTransform.offsetMin;
				m_lastOffsetMax = m_rectTransform.offsetMax;
				m_rectTransform.SetParent(GameObject.Find("Minimize").transform);
				//transform.GetComponent<RectTransform>().sizeDelta = m_resizeWindow.MinimizeSizeWindow;
				State = WindowState.Minimized;
			}
			else if(State == WindowState.Maximized)
			{
				m_rectTransform.SetParent(GameObject.Find("Minimize").transform);
				State = WindowState.Minimized;
			}
			else if(State == WindowState.Minimized)
			{
				m_rectTransform.SetParent(m_parentRectTransform);
				m_rectTransform.anchorMin = m_originalAnchorMinPosition;
				m_rectTransform.anchorMax = m_originalAnchorMaxPosition;
				m_rectTransform.offsetMin = m_lastOffsetMin;
				m_rectTransform.offsetMax = m_lastOffsetMax;
				State = WindowState.Normal;
				showWindowInFirst();
			}
		}
		#endregion

		#region Private Methods
		private void setWindowState(WindowState windowState)
		{
			if(windowState == WindowState.Normal)
			{
				transform.Find("Content").gameObject.SetActive(true);
				//m_resizeWindow.ResizeEnabled = true;
			}
			else if(windowState == WindowState.Minimized)
			{
				transform.Find("Content").gameObject.SetActive(false);
				//m_resizeWindow.ResizeEnabled = false;
			}
			else if(windowState == WindowState.Maximized)
			{
				transform.Find("Content").gameObject.SetActive(true);
				//m_resizeWindow.ResizeEnabled = false;
			}
			m_state = windowState;
		}
		private void calculateMinimumSize()
		{
			//m_resizeWindow.MinimizeSizeWindow = new Vector2((float)m_title.font.fontSize*m_title.text.Length+100.0f,30.0f);
			//transform.GetComponent<LayoutElement>().minHeight = m_resizeWindow.MinimizeSizeWindow.y;
			//transform.GetComponent<LayoutElement>().minWidth = m_resizeWindow.MinimizeSizeWindow.x;
		}

		private void showWindowInFirst()
		{
			transform.SetAsLastSibling();
		}

		private void setInteractble(bool isInteractable)
		{
			transform.Find("Hider").gameObject.SetActive(!isInteractable);
			if(isInteractable)
			{
				transform.SetAsLastSibling();
			}
			else
			{
				transform.SetAsFirstSibling();
			}
		}
		#endregion
	}
}