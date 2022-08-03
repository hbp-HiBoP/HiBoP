using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Window
{
	[RequireComponent (typeof (RectTransform))]
	public class Resizer : MonoBehaviour 
	{
        #region Properties
        [SerializeField] HBP.Theme.Element Cursors;

        [SerializeField] HBP.Theme.State Default;
        [SerializeField] HBP.Theme.State LeftRight;
        [SerializeField] HBP.Theme.State BottomTop;
        [SerializeField] HBP.Theme.State TopLeftToBottomRight;
        [SerializeField] HBP.Theme.State BottomLeftToTopRight;

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
            if (enabled) Cursors.Set(gameObject, LeftRight);
		}	
		public void DisplayBottomTopCursor()
		{
            if (enabled) Cursors.Set(gameObject, BottomTop);

        }
        public void DisplayBottomLeftToTopRightCursor()
		{
            if (enabled) Cursors.Set(gameObject, BottomLeftToTopRight);

        }
        public void DisplayTopLeftToBottomRightCursor()
		{
            if (enabled) Cursors.Set(gameObject, TopLeftToBottomRight);

        }
        public void DisplayNormalCursor()
		{
            if (enabled) Cursors.Set(gameObject, Default);
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

            Vector3[] corners = new Vector3[4];
            m_RectTransform.GetWorldCorners(corners);
            Debug.Log(m_RectTransform.rect);
            Debug.Log(m_RectTransform.sizeDelta);
            Vector2 size = corners[2] - corners[0];

            if(resize.x <= 0) w = Mathf.Sign(resize.x) * Mathf.Min(m_RectTransform.rect.width - m_LayoutElement.minWidth, -resize.x);
            if (resize.y <= 0) h = Mathf.Sign(resize.y) * Mathf.Min(m_RectTransform.rect.height - m_LayoutElement.minHeight, -resize.y);
            Vector2 realResize = new Vector2(w,h);
            m_RectTransform.sizeDelta += realResize;
            float xPivot = 0, yPivot = 0;
            if (sides.x == 1) xPivot = m_RectTransform.pivot.x;
            else if(sides.x == -1) xPivot = 1 - m_RectTransform.pivot.x;
            if(sides.y == 1) yPivot = m_RectTransform.pivot.y;
            else if(sides.y == -1) yPivot = 1 - m_RectTransform.pivot.y;    
            m_RectTransform.anchoredPosition += new Vector2(sides.x * xPivot * realResize.x, sides.y * yPivot * realResize.y);
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
