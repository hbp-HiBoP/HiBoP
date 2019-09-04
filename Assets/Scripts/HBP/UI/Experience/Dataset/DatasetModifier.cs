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
            ItemTemp.SetData(m_DataInfoListGestion.Objects);
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
            m_ProtocolDropdown.onValueChanged.AddListener(OnChangeProtocol);

            m_DataInfoListGestion.Initialize(m_SubWindows);
            m_DataInfoListGestion.OnDataInfoNeedCheckErrors.AddListener(CheckErrors);
            m_DataInfoListGestion.OnAddDataInfo.AddListener((d) => ItemTemp.AddData(d));
            m_DataInfoListGestion.OnRemoveDataInfo.AddListener((d) => ItemTemp.RemoveData(d));
            m_DataInfoListGestion.OnUpdateDataInfo.AddListener((d) =>
            {
                ItemTemp.RemoveData(d);
                ItemTemp.AddData(d);
            });
        }
        protected override void SetFields(d.Dataset objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_ProtocolDropdown.value = ApplicationState.ProjectLoaded.Protocols.IndexOf(objectToDisplay.Protocol);
            m_DataInfoListGestion.Objects = objectToDisplay.Data.ToList();
        }
        protected virtual void OnChangeProtocol(int value)
        {
            ItemTemp.Protocol = ApplicationState.ProjectLoaded.Protocols[value];
            m_DataInfoListGestion.UpdateAllObjects();
        }
        protected virtual void CheckErrors(d.DataInfo dataInfo)
        {
            dataInfo.GetErrors(ItemTemp.Protocol);
        }
        #endregion
    }
}