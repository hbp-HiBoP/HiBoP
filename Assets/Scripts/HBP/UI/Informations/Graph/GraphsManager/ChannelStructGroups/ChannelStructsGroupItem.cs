using HBP.Data.Informations;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class ChannelStructsGroupItem : ActionnableItem<ChannelStructsGroup>
    {
        #region Properties

        [SerializeField] Text m_NameText;
        [SerializeField] Text m_ChannelsText;

        [SerializeField] Theme.State m_ErrorState;

        public override ChannelStructsGroup Object
        {
            get { return base.Object; }
            set
            {
                SetInteractable();

                base.Object = value;
                m_NameText.text = value.Name;
                m_ChannelsText.SetIEnumerableFieldInItem("Sites", value.Channels.Select(c => $"{c.Channel} ({c.Patient.Name})"), m_ErrorState);

                SetNotInteractable();
            }
        }

        #endregion
    }
}
