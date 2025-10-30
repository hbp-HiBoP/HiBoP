using HBP.Data.Informations;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class ChannelItem : SelectableItem<ChannelStruct>
    {
        #region Properties
        [SerializeField] private Text m_NameText;

        public override ChannelStruct Object
        {
            get => base.Object;
            set
            {
                SetInteractable();

                base.Object = value;
                m_NameText.text = value.Channel;

                SetNotInteractable();
            }
        }
        #endregion
    }
}