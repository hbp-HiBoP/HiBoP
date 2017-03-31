using UnityEngine;

namespace Tools.Unity
{
    [RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
    public class VerticalUIFitter : MonoBehaviour
    {
        #region Properties
        RectTransform rect;
        RectTransform container;

        public enum RotationEnum { Left, Right };
        RotationEnum m_rotation;
        public RotationEnum Rotation
        {
            get
            {
                return m_rotation;
            }
            set
            {
                m_rotation = value; Set();
            }
        }

        #endregion
        #region Events
        void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                Fit();
            }
        }

        void Awake()
        {
            Set();
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                Fit();
            }
        }

        void OnTransformParentChanged()
        {
            Set();
        }
        #endregion
        #region Methods
        void Set()
        {
            rect = GetComponent<RectTransform>();
            container = transform.parent.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 0, 0);
            rect.sizeDelta = new Vector2(0, 0);
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            if (Rotation == RotationEnum.Left)
            {
                rect.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
            }
            else
            {
                rect.localRotation = Quaternion.AngleAxis(-90, Vector3.forward);
            }
            Fit();
        }

        void Fit()
        {
            float fit = (container.rect.height - container.rect.width);
            rect.sizeDelta = fit * new Vector2(1, -1);
            if (container.anchorMax.x == container.anchorMin.x && container.anchorMin.y == container.anchorMax.y)
            {
                rect.localPosition = new Vector3(0, container.rect.height / 2);
                rect.localPosition = new Vector3(container.rect.width / 2, 0);
            }
            if (container.anchorMax.x == container.anchorMin.x)
            {
                rect.localPosition = new Vector3(container.rect.width / 2, 0);
            }
            else if (container.anchorMax.y == container.anchorMin.y)
            {
                rect.localPosition = new Vector3(0, container.rect.height / 2);
            }
            else
            {
                rect.localPosition = new Vector3(0, 0, 0);
            }
        }
        #endregion
    }
}