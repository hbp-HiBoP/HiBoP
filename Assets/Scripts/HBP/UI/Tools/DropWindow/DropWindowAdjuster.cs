using HBP.Core.Tools;
using UnityEngine;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class DropWindowAdjuster : MonoBehaviour
    {
        #region Properties
        private RectTransform m_RectTransfom;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_RectTransfom = GetComponent<RectTransform>();
        }
        public void Update()
        {
            if (m_RectTransfom.hasChanged)
            {
                AdjustIfOutOfScreen();
                m_RectTransfom.hasChanged = false;
            }
        }
        private void AdjustIfOutOfScreen()
        {
            // TODO
            float value = m_RectTransfom.position.y - m_RectTransfom.rect.height;
            if (value < 0)
            {
                m_RectTransfom.anchorMin = new Vector2(m_RectTransfom.anchorMin.x, 1);
                m_RectTransfom.anchorMax = new Vector2(m_RectTransfom.anchorMax.x, 1);
                m_RectTransfom.pivot = new Vector2(m_RectTransfom.pivot.x, 0);

                Vector2 newPos = m_RectTransfom.anchoredPosition;
                newPos.y = 1;
                m_RectTransfom.anchoredPosition = newPos;
            }
            else
            {
                m_RectTransfom.anchorMin = new Vector2(m_RectTransfom.anchorMin.x, 0);
                m_RectTransfom.anchorMax = new Vector2(m_RectTransfom.anchorMax.x, 0);
                m_RectTransfom.pivot = new Vector2(m_RectTransfom.pivot.x, 1);

                Vector2 newPos = m_RectTransfom.anchoredPosition;
                newPos.y = -1;
                m_RectTransfom.anchoredPosition = newPos;
            }
        }

        #endregion
    }
}