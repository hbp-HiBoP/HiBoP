using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
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
        #endregion
    }
}