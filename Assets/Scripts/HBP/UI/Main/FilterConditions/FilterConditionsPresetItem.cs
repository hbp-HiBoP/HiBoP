using HBP.Core.Data;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class FilterConditionsPresetItem : ActionnableItem<FilterConditionsPreset>
    {
        #region Properties

        [SerializeField] Text m_NameText;
        [SerializeField] Text m_ConditionsText;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override FilterConditionsPreset Object
        {
            get { return base.Object; }
            set
            {
                SetInteractable();

                base.Object = value;
                m_NameText.text = value.Name;
                m_ConditionsText.text = value.Conditions.Count.ToString();

                SetNotInteractable();
            }
        }

        #endregion
    }
}
