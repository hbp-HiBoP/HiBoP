using HBP.Core.Tools;
using UnityEngine;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class DropWindowAdjuster : MonoBehaviour
    {
        #region Properties
        private RectTransform m_RectTransform;
        private RectTransform m_Canvas;

        private float m_OldHeight;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_Canvas = m_RectTransform.GetTopmostCanvas().GetComponent<RectTransform>();
        }
        private void Update()
        {
            if (m_OldHeight != m_RectTransform.rect.height)
            {
                AdjustIfOutOfScreen();
                m_RectTransform.hasChanged = false;
            }
        }
        private void OnEnable()
        {
            AdjustIfOutOfScreen();
        }
        private void OnDisable()
        {
            ApplyBottomPosition();
        }
        private void AdjustIfOutOfScreen()
        {
            if (CheckLimits())
            {
                ApplyTopPosition();
            }
            else
            {
                ApplyBottomPosition();
                if (CheckLimits())
                {
                    ApplyTopPosition();
                }
            }

            m_OldHeight = m_RectTransform.rect.height;
        }
        private bool CheckLimits()
        {
            Vector3[] worldCorners = new Vector3[4];
            m_RectTransform.GetWorldCorners(worldCorners);

            Vector3[] canvasCorners = new Vector3[4];
            m_Canvas.GetWorldCorners(canvasCorners);

            float windowBottom = worldCorners[0].y;
            float canvasBottom = canvasCorners[0].y;

            return windowBottom < canvasBottom;
        }
        private void ApplyTopPosition()
        {
            m_RectTransform.anchorMin = new Vector2(m_RectTransform.anchorMin.x, 1);
            m_RectTransform.anchorMax = new Vector2(m_RectTransform.anchorMax.x, 1);
            m_RectTransform.pivot = new Vector2(m_RectTransform.pivot.x, 0);
            Vector2 newPos = m_RectTransform.anchoredPosition;
            newPos.y = 1;
            m_RectTransform.anchoredPosition = newPos;
        }
        private void ApplyBottomPosition()
        {
            m_RectTransform.anchorMin = new Vector2(m_RectTransform.anchorMin.x, 0);
            m_RectTransform.anchorMax = new Vector2(m_RectTransform.anchorMax.x, 0);
            m_RectTransform.pivot = new Vector2(m_RectTransform.pivot.x, 1);
            Vector2 newPos = m_RectTransform.anchoredPosition;
            newPos.y = -1;
            m_RectTransform.anchoredPosition = newPos;
        }
        #endregion
    }
}