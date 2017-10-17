using UnityEngine;
using System.Collections;

 namespace Tools.Unity.Graph
{
    [RequireComponent(typeof(PlotGestion))]
    public class Listener : MonoBehaviour
    {
        #region Properties
        PlotGestion plotGestion;
        RectTransform rectTransform;

        float lastHeight;
        float lastWidth;
        Vector2 mouseLastPosition;
        bool firstUse = true;
        #endregion

        #region Public Methods
        public void OnBeginDrag()
        {
            mouseLastPosition = Input.mousePosition;
        }

        public void OnDrag()
        {
            Vector2 mouseActualPosition = Input.mousePosition;
            Vector2 displacement = mouseActualPosition - mouseLastPosition;
            Vector2 proportionnalDisplacement = new Vector2(displacement.x / rectTransform.rect.width, displacement.y / rectTransform.rect.height);
            plotGestion.Move(proportionnalDisplacement);
            mouseLastPosition = mouseActualPosition;
        }

        public void OnScroll()
        {
            float l_scroll = Input.mouseScrollDelta.y;
            if(l_scroll > 0)
            {
                plotGestion.Zoom();
            }
            else if(l_scroll < 0)
            {
                plotGestion.Dezoom();
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            plotGestion = GetComponent<PlotGestion>();
            rectTransform = GetComponent<RectTransform>();
            lastHeight = rectTransform.rect.height;
            lastWidth = rectTransform.rect.width;
        }

        void OnRectTransformDimensionsChange()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(c_OnRect());
            }
        }

        IEnumerator c_OnRect()
        {
            yield return new WaitForEndOfFrame();
            OnRect();
        }

        void OnRect()
        {
            Rect l_actualRect = rectTransform.rect;
            if (lastHeight != l_actualRect.height || lastWidth != l_actualRect.width)
            {
                if (firstUse)
                {
                    lastHeight = l_actualRect.height;
                    lastWidth = l_actualRect.width;
                    firstUse = false;
                }
                else
                {
                    Vector2 l_command = Vector2.zero;
                    l_command.x = (l_actualRect.width - lastWidth) / lastWidth;
                    l_command.y = (l_actualRect.height - lastHeight) / lastHeight;
                    lastHeight = l_actualRect.height;
                    lastWidth = l_actualRect.width;
                    plotGestion.ChangeRectSize(l_command);
                }
            }
        }
        #endregion
    }
}