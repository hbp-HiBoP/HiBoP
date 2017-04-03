using UnityEngine;

namespace Tools.Unity.Window
{
	[RequireComponent (typeof (RectTransform))]
	public class DragWindow : MonoBehaviour
	{
        #region Attributs
        private Vector3 m_initialDifferencePosition;
		private RectTransform m_panelRectTransform;
		private RectTransform m_parentRectTransform;
		public bool m_dragEnabled = true;
		public bool DragEnabled {get{return m_dragEnabled;}set{m_dragEnabled = value;}}
		#endregion

		#region Event
        public void OnBeginDrag()
        {
            m_initialDifferencePosition = transform.position - Input.mousePosition;
        }

        public void OnDrag()
        {
            transform.position = Input.mousePosition + m_initialDifferencePosition;
            ClampToWindow();
        }
		#endregion

		#region Private Methods
		// Clamp panel to area of parent
		private void ClampToWindow () 
		{
            if(m_panelRectTransform == null) m_panelRectTransform = transform as RectTransform;
            if(m_parentRectTransform == null) m_parentRectTransform = m_panelRectTransform.parent as RectTransform;

            Vector3 l_pos = m_panelRectTransform.localPosition;
			Vector3 l_minPosition = m_parentRectTransform.rect.min - m_panelRectTransform.rect.min;
			Vector3 l_maxPosition = m_parentRectTransform.rect.max - m_panelRectTransform.rect.max;
			
			l_pos.x = Mathf.Clamp (m_panelRectTransform.localPosition.x, l_minPosition.x, l_maxPosition.x);
			l_pos.y = Mathf.Clamp (m_panelRectTransform.localPosition.y, l_minPosition.y, l_maxPosition.y);
			
			m_panelRectTransform.localPosition = l_pos;
		}
		#endregion
	}
}
