using HBP.Data.Informations;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class ChannelStructItem : SelectableItem<ChannelStruct>
    {
        #region Properties

        [SerializeField] Text m_ChannelText;
        [SerializeField] Text m_PatientText;

        public override ChannelStruct Object
        {
            get { return base.Object; }
            set
            {
                SetInteractable();

                base.Object = value;
                m_ChannelText.text = value.Channel;
                m_PatientText.text = value.Patient.Name;

                SetNotInteractable();
            }
        }

        #endregion
    }
}
