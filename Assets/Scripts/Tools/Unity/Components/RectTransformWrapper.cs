using UnityEngine;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
    public class RectTransformWrapper : MonoBehaviour
    {
        #region Public Methods
        public void SetXPosition(float x)
        {
            RectTransform rectTransform = transform as RectTransform;
            x = float.IsNaN(x) ? -1 : x;
            rectTransform.anchorMin = new Vector2(x, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(x, rectTransform.anchorMax.y);
        }
        public void SetYPosition(float y)
        {
            RectTransform rectTransform = transform as RectTransform;
            y = float.IsNaN(y) ? -1 : y;
            rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, y);
            rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, y);
        }
        #endregion
    }
}