using UnityEngine;

namespace Tools.Unity.Window
{
	[RequireComponent (typeof (RectTransform))]
	public class ResizeWindow : MonoBehaviour 
	{
		#region Attributs
		/* Texture Cursor + offsetPosition Cursor */
		[SerializeField]
		private Texture2D m_horizontalCursorTexture;
		[SerializeField]
		private Texture2D m_verticalCursorTexture;
		[SerializeField]
		private Texture2D m_diagonalCursorTexture1;
		[SerializeField]
		private Texture2D m_diagonalCursorTexture2;
		[SerializeField]
		private Vector2 m_cursorOffsetPosition = new Vector2(11,11);

		/* RectTransforms */
		private RectTransform m_rectTransform;

		/* Vector2 initial position + offset */
		private Vector2 m_minimizeSizeWindow;
		public Vector2 MinimizeSizeWindow{get{return m_minimizeSizeWindow;}set{m_minimizeSizeWindow = value;}}
		public Vector2 MinSizeWindow;

        /* Vector2 last postion */
        private Vector2 m_initialRectSize;
        private Vector2 m_initialSizeDelta;
        private Vector3 m_initialPanelPosition;
        private Vector3 m_initialMousePosition;

        /* Enabled */
        [SerializeField]
        bool CanBeResizedVertical = true;
        [SerializeField]
        bool CanBeResizedHorizontal = true;

        private bool m_resizeEnabled = true;
		public bool ResizeEnabled
        {
            get { return m_resizeEnabled;  }
            set { m_resizeEnabled = value; }
        }
        #endregion

        #region Initialisation
        void OnEnable()
		{
            m_rectTransform = transform.GetComponent<RectTransform>();
            float difH = MinSizeWindow.y - m_rectTransform.rect.height;
            float difW = MinSizeWindow.x - m_rectTransform.rect.width;
            if (difH > 0)
            {
                m_rectTransform.sizeDelta += new Vector2(0, difH);
                m_rectTransform.localPosition = new Vector2(0, 0);
            }
            if (difW > 0)
            {
                m_rectTransform.sizeDelta += new Vector2(difW, 0);
                m_rectTransform.localPosition = new Vector2(0, 0);
            }
        }
		#endregion

		#region Event
		public void OnHorizontalMouseEnter()
		{
			if(ResizeEnabled && CanBeResizedHorizontal)
			{
				Cursor.SetCursor(m_horizontalCursorTexture,m_cursorOffsetPosition,CursorMode.ForceSoftware);
			}
		}
		
		public void OnVerticalMouseEnter()
		{
			if(ResizeEnabled && CanBeResizedVertical)
			{
				Cursor.SetCursor(m_verticalCursorTexture,m_cursorOffsetPosition,CursorMode.ForceSoftware);
			}
		}
		
		public void OnDiagonal1Enter()
		{
			if(ResizeEnabled && CanBeResizedHorizontal && CanBeResizedVertical)
			{
				Cursor.SetCursor(m_diagonalCursorTexture1,m_cursorOffsetPosition,CursorMode.ForceSoftware);
			}
		}
		
		public void OnDiagonal2Enter()
		{
            if (ResizeEnabled && CanBeResizedHorizontal && CanBeResizedVertical)
            {
                Cursor.SetCursor(m_diagonalCursorTexture2,m_cursorOffsetPosition,CursorMode.ForceSoftware);
			}
		}
		
		public void OnMouseExit()
		{
				Cursor.SetCursor(null,new Vector2(0,0),CursorMode.ForceSoftware);
		}
		
		public void OnBeginDrag()
		{
                m_initialPanelPosition = m_rectTransform.position;
                m_initialSizeDelta = m_rectTransform.sizeDelta;
                m_initialRectSize = m_rectTransform.rect.size;
                m_initialMousePosition = Input.mousePosition;
		}
		
		public void OnHorizontalDrag(bool isLeft)
		{
            if (ResizeEnabled && CanBeResizedHorizontal)
            {
                int l_side;
                if (!isLeft) l_side = 1;
                else l_side = -1;
                float l_resize = l_side * (Input.mousePosition.x - m_initialMousePosition.x);
                Vector2 l_newSize = new Vector2(m_initialSizeDelta.x + l_resize, m_initialSizeDelta.y);
                if ((m_initialRectSize.x + l_resize) < MinSizeWindow.x)
                {
                    l_newSize.x = m_initialSizeDelta.x + MinSizeWindow.x - m_initialRectSize.x;
                }
                m_rectTransform.sizeDelta = l_newSize;
                m_rectTransform.position = m_initialPanelPosition + l_side * 0.5f * new Vector3(l_newSize.x - m_initialSizeDelta.x, 0, 0);
            }
		}
		
		public void  OnVerticalDrag(bool isBot)
		{
            if (ResizeEnabled && CanBeResizedVertical)
            {
                int l_side;
                if (!isBot) l_side = 1;
                else l_side = -1;
                float l_resize = l_side * (Input.mousePosition.y - m_initialMousePosition.y);
                Vector2 l_newSize = new Vector2(m_initialSizeDelta.x, m_initialSizeDelta.y + l_resize);
                if ((m_initialRectSize.y + l_resize) < MinSizeWindow.y)
                {
                    l_newSize.y = m_initialSizeDelta.y + MinSizeWindow.y - m_initialRectSize.y;
                }
                m_rectTransform.sizeDelta = l_newSize;
                m_rectTransform.position = m_initialPanelPosition + l_side*0.5f * new Vector3(0, l_newSize.y - m_initialSizeDelta.y, 0);
            }
        }
		
		public void OnDiagonalDrag(int i)
		{
            if (ResizeEnabled && CanBeResizedHorizontal && CanBeResizedVertical)
            {
                int l_sideRL = 0;
                int l_sideTB = 0;
                if (i == 0)
                {
                    l_sideRL = -1;
                    l_sideTB = -1;
                }
                else if (i == 1)
                {
                    l_sideRL = 1;
                    l_sideTB = -1;
                }
                else if (i == 2)
                {
                    l_sideRL = 1;
                    l_sideTB = 1;
                }
                else if (i == 3)
                {
                    l_sideRL = -1;
                    l_sideTB = 1;
                }

                Vector2 l_resize = new Vector2(l_sideRL*(Input.mousePosition.x - m_initialMousePosition.x),l_sideTB*(Input.mousePosition.y-m_initialMousePosition.y));
                Vector2 l_newSize = m_initialSizeDelta + l_resize;
                if ((m_initialRectSize.x + l_resize.x) < MinSizeWindow.x)
                {
                    l_newSize.x = m_initialSizeDelta.x + MinSizeWindow.x - m_initialRectSize.x;
                }
                if ((m_initialRectSize.y + l_resize.y) < MinSizeWindow.y)
                {
                    l_newSize.y = m_initialSizeDelta.y + MinSizeWindow.y - m_initialRectSize.y;
                }
                m_rectTransform.sizeDelta = l_newSize;
                m_rectTransform.position = m_initialPanelPosition + 0.5f * new Vector3(l_sideRL*(l_newSize.x-m_initialSizeDelta.x), l_sideTB * (l_newSize.y - m_initialSizeDelta.y), 0);
            }
        }
		#endregion
	}
}
