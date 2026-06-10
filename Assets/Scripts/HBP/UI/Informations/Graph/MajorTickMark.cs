using System.CodeDom;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;
using HBP.Core.Tools;

namespace HBP.UI.Informations.Graphs
{
    public class MajorTickMark : TickMark
    {
        #region Properties
        [SerializeField] protected string m_Label;
        public virtual string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Label, value))
                {
                    SetLabel();
                }
            }
        }

        [SerializeField] protected StringEvent m_OnChangeLabel;
        public StringEvent OnChangeLabel
        {
            get
            {
                return m_OnChangeLabel;
            }
        }

        [SerializeField] protected bool m_ShowLabel;
        public bool ShowLabel
        {
            get
            {
                return m_ShowLabel;
            }
            set
            {

                if (SetPropertyUtility.SetStruct(ref m_ShowLabel, value))
                {
                    SetHidden();
                }
            }
        }

        [SerializeField] protected BoolEvent m_OnChangeShowLabel;
        public BoolEvent OnChangeShowLabel
        {
            get
            {
                return m_OnChangeShowLabel;
            }
        }

        [SerializeField] protected float m_Value;
        public float Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Value, value))
                {
                    SetValue();
                }
            }
        }

        [SerializeField] protected FloatEvent m_OnChangeValue;
        public FloatEvent OnChangeValue
        {
            get
            {
                return m_OnChangeValue;
            }
        }

        [SerializeField] protected BoolEvent m_OnChangeShow;
        public BoolEvent OnChangeShow
        {
            get
            {
                return m_OnChangeShow;
            }
        }
        #endregion

        #region Protected Setters
        protected override void OnValidate()
        {
            base.OnValidate();
            SetLabel();
            SetHidden();
        }
        protected void SetLabel()
        {
            m_OnChangeLabel.Invoke(m_Label);
        }
        protected void SetHidden()
        {
            m_OnChangeShowLabel.Invoke(m_ShowLabel);
        }
        protected void SetValue()
        {
            m_OnChangeValue.Invoke(m_Value);
        }
        #endregion

        #region Public Methods
        public void Show()
        {
            gameObject.SetActive(true);
            m_OnChangeShow.Invoke(true);
        }
        public void Hide()
        {
            gameObject.SetActive(false);
            m_OnChangeShow.Invoke(false);
        }
        #endregion
    }
}