using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Linq;

namespace HBP.UI.Experience.Dataset
{
	/// <summary>
	/// Display/Modify dataset.
	/// </summary>
	public class DatasetModifier : ItemModifier<d.Dataset> 
	{
        #region Properties		
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_ProtocolDropdown;

        [SerializeField] Button m_CreateButton, m_RemoveButton;
        [SerializeField] DataInfoListGestion m_DataInfoListGestion;

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
                m_ProtocolDropdown.interactable = value;

                m_CreateButton.interactable = value;
                m_RemoveButton.interactable = value;
                m_DataInfoListGestion.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            ItemTemp.SetData(m_DataInfoListGestion.Items);
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);

            m_ProtocolDropdown.options = (from protocol in ApplicationState.ProjectLoaded.Protocols select new Dropdown.OptionData(protocol.Name)).ToList();
            m_ProtocolDropdown.onValueChanged.RemoveAllListeners();
            m_ProtocolDropdown.onValueChanged.AddListener((value) => ItemTemp.Protocol = ApplicationState.ProjectLoaded.Protocols[value]);

            m_DataInfoListGestion.Initialize(m_SubWindows);
        }
        protected override void SetFields(d.Dataset objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_ProtocolDropdown.value = ApplicationState.ProjectLoaded.Protocols.IndexOf(objectToDisplay.Protocol);
            m_DataInfoListGestion.Items = objectToDisplay.Data.ToList();
        }
        #endregion
    }
}