using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;
using HBP.UI.Theme;

namespace Tools.Unity.Window
{
	[RequireComponent (typeof (RectTransform))]
	public class Resizer : MonoBehaviour 
	{
		#region Properties
		/* RectTransforms */
		RectTransform m_RectTransform;
        ILayoutElement m_LayoutElement;
        #endregion

        #region Initialisation
        void OnEnable()
		{
            m_RectTransform = GetComponent<RectTransform>();
            m_LayoutElement = GetComponent<ILayoutElement>();
        }
		#endregion

		#region Public Methods
		public void DisplayLeftRightCursor()
		{
            Theme.CursorTheme theme = ApplicationState.GeneralSettings.Theme.General.LeftRightCursor;
            if (isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(theme.Texture, theme.Offset, CursorMode.Auto);
		}	
		public void DisplayBottomTopCursor()
		{
            Theme.CursorTheme theme = ApplicationState.GeneralSettings.Theme.General.TopBottomCursor;
            if (isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(theme.Texture, theme.Offset, CursorMode.Auto);
        }
        public void DisplayBottomLeftToTopRightCursor()
		{
            Theme.CursorTheme theme = ApplicationState.GeneralSettings.Theme.General.BottomLeftTopRightCursor;
            if (isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(theme.Texture, theme.Offset, CursorMode.Auto);
        }
        public void DisplayTopLeftToBottomRightCursor()
		{
            Theme.CursorTheme theme = ApplicationState.GeneralSettings.Theme.General.TopLeftBottomRightCursor;
            if (isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(theme.Texture, theme.Offset, CursorMode.Auto);
        }
        public void DisplayNormalCursor()
		{
            if(!Input.GetMouseButton(0))
            {
                Cursor.SetCursor(ApplicationState.GeneralSettings.Theme.General.Cursor.Texture, ApplicationState.GeneralSettings.Theme.General.Cursor.Offset, CursorMode.Auto);
            }   
        }			

        public void LeftDrag()
        {
            if(isActiveAndEnabled)
            {
                Vector2 resize = new Vector2(GetLimits().xMin - Input.mousePosition.x,0);
                Vector2 sides = new Vector2(-1, 0);
                Resize(resize,sides);
            }
        }
        public void RightDrag()
        {
            if (isActiveAndEnabled)
            {
                Vector2 resize = new Vector2(Input.mousePosition.x - GetLimits().xMax, 0);
                Vector2 sides = new Vector2(1, 0);
                Resize(resize, sides);
            }
        }
        public void TopDrag()
        {
            if (isActiveAndEnabled)
            {
                Vector2 resize = new Vector2(0, Input.mousePosition.y - GetLimits().yMax);
                Vector2 sides = new Vector2(0, 1);
                Resize(resize, sides);
            }
        }
        public void BottomDrag()
        {
            if (isActiveAndEnabled)
            {
                Vector2 resize = new Vector2(0, GetLimits().yMin - Input.mousePosition.y);
                Vector2 sides = new Vector2(0, -1);
                Resize(resize, sides);
            }
        }

        public void BottomLeftDrag()
        {
            if (isActiveAndEnabled)
            {
                Limits limits = GetLimits();
                Vector2 resize = new Vector2(limits.xMin - Input.mousePosition.x, limits.yMin - Input.mousePosition.y);
                Vector2 sides = new Vector2(-1, -1);
                Resize(resize, sides);
            }
        }
        public void TopRightDrag()
        {
            if (isActiveAndEnabled)
            {
                Limits limits = GetLimits();
                Vector2 resize = new Vector2(Input.mousePosition.x - limits.xMax, Input.mousePosition.y - limits.yMax);
                Vector2 sides = new Vector2(1, 1);
                Resize(resize, sides);
            }
        }
        public void TopLeftDrag()
        {
            if (isActiveAndEnabled)
            {
                Limits limits = GetLimits();
                Vector2 resize = new Vector2(limits.xMin - Input.mousePosition.x, Input.mousePosition.y - limits.yMax);
                Vector2 sides = new Vector2(-1, 1);
                Resize(resize, sides);
            }
        }
        public void BottomRightDrag()
        {
            if (isActiveAndEnabled)
            {
                Limits limits = GetLimits();
                Vector2 resize = new Vector2(Input.mousePosition.x - limits.xMax, limits.yMin - Input.mousePosition.y);
                Vector2 sides = new Vector2(1, -1);
                Resize(resize, sides);
            }
        }
        #endregion

        #region Private Methods
        private void Clamp()
        {
        }
        void Resize(Vector2 resize, Vector2 sides)
        {
            float w = resize.x;
            float h = resize.y;
            if(resize.x <= 0)
            {
                w = Mathf.Sign(resize.x) * Mathf.Min(m_RectTransform.rect.width - m_LayoutElement.minWidth, -resize.x);
            }
            if (resize.y <= 0)
            {
                h = Mathf.Sign(resize.y) * Mathf.Min(m_RectTransform.rect.height - m_LayoutElement.minHeight, -resize.y);
            }
            Vector2 realResize = new Vector2(w,h);
            m_RectTransform.sizeDelta += realResize;
            m_RectTransform.position += new Vector3(sides.x * m_RectTransform.pivot.x * realResize.x, sides.y * m_RectTransform.pivot.y * realResize.y, 0);
        }
        Limits GetLimits()
        {
            Vector3[] corners = new Vector3[4];
            m_RectTransform.GetWorldCorners(corners);
            Vector2 BottomLeftCorner = RectTransformUtility.WorldToScreenPoint(GetComponentInParent<Canvas>().worldCamera, corners[0]);
            Vector2 TopRightCorner = RectTransformUtility.WorldToScreenPoint(GetComponentInParent<Canvas>().worldCamera, corners[2]);
           
            return new Limits(BottomLeftCorner.x,TopRightCorner.x,BottomLeftCorner.y,TopRightCorner.y);
        }
        #endregion

        #region Struct
        struct Limits
        {
            public float xMin;
            public float xMax;
            public float yMin;
            public float yMax;

            public Limits(float xmin,float xmax,float ymin, float ymax)
            {
                xMin = xmin;
                xMax = xmax;
                yMin = ymin;
                yMax = ymax;
            }
        }
        #endregion  
    }
}
