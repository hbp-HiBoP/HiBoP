using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolModifier : ItemModifier<d.Protocol> 
	{
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] BlocListGestion m_BlocListGestion;
        [SerializeField] Button m_CreateBlocButton;
        [SerializeField] Button m_RemoveBlocButton;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;

                m_BlocListGestion.Interactable = value;
                m_CreateBlocButton.interactable = value;
                m_RemoveBlocButton.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields(d.Protocol objectToDisplay)
        {
            m_BlocListGestion.Initialize(m_SubWindows);
            m_BlocListGestion.Objects =  objectToDisplay.Blocs;

            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onEndEdit.AddListener((value) => objectToDisplay.Name = value);

            base.SetFields();
        }
        #endregion
    }
}