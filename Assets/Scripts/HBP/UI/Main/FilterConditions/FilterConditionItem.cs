using HBP.Core.Data;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class FilterConditionItem : ActionnableItem<BaseFilterCondition>
    {
        #region Properties
        [SerializeField] Text m_DescriptionText;

        public override BaseFilterCondition Object
        {
            get => base.Object;
            set
            {
                SetInteractable();

                base.Object = value;
                m_DescriptionText.text = value.Description;

                SetNotInteractable();
            }
        }
        #endregion
    }
}