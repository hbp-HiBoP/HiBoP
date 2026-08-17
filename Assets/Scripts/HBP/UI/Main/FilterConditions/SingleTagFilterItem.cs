using HBP.Core.Data;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class SingleTagFilterItem : ActionnableItem<SingleTagFilter>
    {
        #region Properties

        [SerializeField] Text m_Text;

        public override SingleTagFilter Object
        {
            get => base.Object;
            set
            {
                SetInteractable();

                base.Object = value;
                m_Text.text = Object?.Description ?? "No tag selected";

                SetNotInteractable();
            }
        }

        #endregion
    }
}
