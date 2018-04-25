using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteConditionItem : SelectableItem<SiteCondition>
    {
        #region Properties
        [SerializeField] private Dropdown m_Target;
        [SerializeField] private Dropdown m_Comparator;
        [SerializeField] private InputField m_Value;

        public override SiteCondition Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                UpdateFields();
            }
        }

        public int Target
        {
            get
            {
                return (int)Object.Target;
            }
            set
            {
                Object.Target = (SiteCondition.ConditionTarget)value;
            }
        }
        public int Comparator
        {
            get
            {
                return (int)Object.Comparator;
            }
            set
            {
                Object.Comparator = (SiteCondition.ConditionComparator)value;
            }
        }
        public string Value
        {
            get
            {
                return Object.Value.ToString("N2");
            }
            set
            {
                float val = 0f;
                if (float.TryParse(value, out val))
                {
                    Object.Value = val;
                }
                else
                {
                    val = Object.Value;
                }
                m_Value.text = val.ToString("N2");
            }
        }
        #endregion

        #region Private Methods
        private void UpdateFields()
        {
            m_Target.ClearOptions();
            m_Target.AddOptions((from targetName in System.Enum.GetNames(typeof(SiteCondition.ConditionTarget)) select new Dropdown.OptionData(targetName)).ToList());
            m_Comparator.ClearOptions();
            m_Comparator.options.Add(new Dropdown.OptionData(">"));
            m_Comparator.options.Add(new Dropdown.OptionData("<"));
            m_Target.value = Target;
            m_Comparator.value = Comparator;
            m_Value.text = Value;
        }
        #endregion
    }
}