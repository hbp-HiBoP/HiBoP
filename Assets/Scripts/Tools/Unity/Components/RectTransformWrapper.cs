using UnityEngine;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformWrapper : MonoBehaviour
    {
        #region Properties
        RectTransform rectTransform;

        #endregion

        #region Public Methods
        public void SetXPosition(float x)
        {
            rectTransform.anchorMin = new Vector2(x, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(x, rectTransform.anchorMax.y);
        }
        public void SetYPosition(float y)
        {
            rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, y);
            rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, y);
        }
        #endregion

        #region Private Methods
        void Start()
        {
            rectTransform = GetComponent<RectTransform>();

        }
        #endregion
    }
}