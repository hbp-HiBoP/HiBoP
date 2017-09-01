using UnityEngine;
using UnityEngine.UI;
using Tools.CSharp;

namespace Tools.Unity.Window
{
	[RequireComponent (typeof (RectTransform))]
	public class Resizer : MonoBehaviour 
	{
		#region Properties
		/* Texture Cursor + offsetPosition Cursor */
		[SerializeField] Texture2D m_WECursor;
		[SerializeField] Texture2D m_NSCursor;
		[SerializeField] Texture2D m_SWNECursor;
		[SerializeField] Texture2D m_NWSECursor;
		[SerializeField] Vector2 m_CursorOffsetPosition = new Vector2(11,11);

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
			if(isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(m_WECursor, m_CursorOffsetPosition, CursorMode.ForceSoftware);
		}	
		public void DisplayBottomTopCursor()
		{
			if(isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(m_NSCursor, m_CursorOffsetPosition, CursorMode.ForceSoftware);
		}		
		public void DisplayBottomLeftToTopRightCursor()
		{
			if(isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(m_SWNECursor, m_CursorOffsetPosition, CursorMode.ForceSoftware);
		}		
		public void DisplayTopLeftToBottomRightCursor()
		{
            if (isActiveAndEnabled && !Input.GetMouseButton(0)) Cursor.SetCursor(m_NWSECursor, m_CursorOffsetPosition, CursorMode.ForceSoftware);
		}		
		public void DisplayNormalCursor()
		{
            if(!Input.GetMouseButton(0))
            {
                Cursor.SetCursor(null, new Vector2(0, 0), CursorMode.ForceSoftware);
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
        void Resize(Vector2 resize, Vector2 sides)
        {
            float w = resize.x;
            float h = resize.y;
            if(resize.x < 0)
            {
                w = Mathf.Sign(resize.x) * Mathf.Min(m_RectTransform.rect.width - m_LayoutElement.minWidth, -resize.x);
            }
            if (resize.y < 0)
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
