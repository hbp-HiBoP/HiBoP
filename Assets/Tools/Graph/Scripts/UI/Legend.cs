using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class Legend : MonoBehaviour
    {
        #region Properties
        [SerializeField] string m_Label;
        public string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Label, value))
                {
                    SetLabel();
                }
            }
        }

        [SerializeField] Color m_Color;
        public Color Color
        {
            get
            {
                return m_Color;
            }
            set
            {
                if (SetPropertyUtility.SetColor(ref m_Color, value))
                {
                    SetColor();
                }
            }
        }

        [SerializeField] Color m_DisabledColor;
        public Color DisabledColor
        {
            get
            {
                return m_DisabledColor;
            }
            set
            {
                if (SetPropertyUtility.SetColor(ref m_DisabledColor, value))
                {
                    SetColor();
                }
            }
        }

        [SerializeField] bool m_IsActive;
        public bool IsActive
        {
            get
            {
                return m_IsActive;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_IsActive, value))
                {
                    SetIsActive();
                }
            }
        }

        [SerializeField] string m_ID;
        public string ID
        {
            get
            {
                return m_ID;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_ID, value);
            }
        }

        //[SerializeField] List<Legend> m_SubLegends;
        //public List<Legend> SubLegends
        //{
        //    get
        //    {
        //        return new ReadOnlyCollection<Legend>(m_SubLegends);
        //    }
        //}

        [SerializeField] RectTransform m_Container;
        public RectTransform Container
        {
            get
            {
                return m_Container;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Container, value))
                {
                    SetContainer();
                }
            }
        }

        [SerializeField] StringEvent m_OnChangeLabel;
        public StringEvent OnChangeLabel
        {
            get
            {
                return m_OnChangeLabel;
            }
        }

        [SerializeField] ColorEvent m_OnChangeColor;
        public ColorEvent OnChangeColor
        {
            get
            {
                return m_OnChangeColor;
            }
        }

        [SerializeField] BoolEvent m_OnChangeIsActive;
        public BoolEvent OnChangeIsActive
        {
            get
            {
                return m_OnChangeIsActive;
            }
        }
        #endregion

        #region Public Methods
        //public void AddSubLegend(Legend subLegend)
        //{
        //    m_SubLegends.Add(subLegend);
        //    subLegend.transform.SetParent(Container);
        //}
        //public void RemoveSubLegend(Legend subLegend)
        //{
        //    if (m_SubLegends.Remove(subLegend))
        //    {
        //        Destroy(subLegend.gameObject);
        //    }
        //}
        #endregion

        #region Private Methods
        private void OnValidate()
        {
            SetLabel();
            SetIsActive();
            SetColor();
        }
        #endregion

        #region Setters
        void SetColor()
        {
            m_OnChangeColor.Invoke(m_IsActive ? m_Color : m_DisabledColor);
        }
        void SetLabel()
        {
            m_OnChangeLabel.Invoke(m_Label);
        }
        void SetIsActive()
        {
            m_OnChangeIsActive.Invoke(m_IsActive);
            SetColor();
            SetContainer();
        }
        void SetContainer()
        {
            m_Container.gameObject.SetActive(m_IsActive);
        }
        #endregion
    }
}