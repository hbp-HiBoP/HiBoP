using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Components
{
    public class MinimumSizeTester : MonoBehaviour
    {
        #region Properties
        [SerializeField] bool m_UseMinWidth;
        public bool UseMinWidth
        {
            get
            {
                return m_UseMinWidth;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_UseMinWidth, value))
                {
                    SetUseMinWidth();
                }
            }
        }

        [SerializeField] bool m_UseMinHeight;
        public bool UseMinHeight
        {
            get
            {
                return m_UseMinHeight;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_UseMinHeight, value))
                {
                    SetUseMinHeight();
                }
            }
        }

        [SerializeField] float m_MinWidth;
        public float MinWidth
        {
            get
            {
                return m_MinWidth;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_MinWidth, value))
                {
                    SetMinWidth();
                }
            }
        }

        [SerializeField] float m_MinHeight;
        public float MinHeight
        {
            get
            {
                return m_MinHeight;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_MinHeight, value))
                {
                    SetMinHeight();
                }
            }
        }

        [SerializeField, ReadOnly] bool m_Minimized;
        public bool Minimized
        {
            get
            {
                return m_Minimized;
            }
        }

        [SerializeField] BoolEvent m_OnChangeMinimized;
        public BoolEvent OnChangeMinimized
        {
            get
            {
                return m_OnChangeMinimized;
            }
        }

        RectTransform m_RectTransform;
        #endregion

        #region Private Methods
        void Start()
        {

        }
        void Update()
        {

        }
        void OnEnable()
        {
            SetMinimized();
        }
        void OnValidate()
        {
            SetMinimized();
        }
        void OnRectTransformDimensionsChange()
        {
            SetMinimized();
        }
        #endregion

        #region Setters
        void SetMinWidth()
        {
            if(m_UseMinWidth)
            {
                SetMinimized();
            }
        }
        void SetMinHeight()
        {
            if(m_UseMinHeight)
            {
                SetMinimized();
            }
        }
        void SetUseMinWidth()
        {
            SetMinimized();
        }
        void SetUseMinHeight()
        {
            SetMinimized();
        }
        void SetMinimized()
        {
            if(isActiveAndEnabled)
            {
                m_RectTransform = transform as RectTransform;
                bool widthIsOk = !m_UseMinWidth || m_RectTransform.rect.width >= m_MinWidth;
                bool heightIsOk = !m_UseMinHeight || m_RectTransform.rect.height >= m_MinHeight;
                bool newValue = !widthIsOk || !heightIsOk;
                if (newValue != m_Minimized)
                {
                    m_Minimized = newValue;
                    m_OnChangeMinimized.Invoke(m_Minimized);
                }
            }
        }
        #endregion
    }
}