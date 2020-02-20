using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Linq;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// Display/Modify dataset.
    /// </summary>
    public class DatasetModifier : ObjectModifier<d.Dataset>
    {
        #region Properties		
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_ProtocolDropdown;

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

                m_DataInfoListGestion.Interactable = value;
                m_DataInfoListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);

            m_ProtocolDropdown.options = (from protocol in ApplicationState.ProjectLoaded.Protocols select new Dropdown.OptionData(protocol.Name)).ToList();
            m_ProtocolDropdown.onValueChanged.AddListener(OnChangeProtocol);

            m_DataInfoListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_DataInfoListGestion.OnDataInfoNeedCheckErrors.AddListener(CheckErrors);
            m_DataInfoListGestion.List.OnAddObject.AddListener(OnAddData);
            m_DataInfoListGestion.List.OnRemoveObject.AddListener(OnRemoveData);
            m_DataInfoListGestion.List.OnUpdateObject.AddListener(OnUpdateData);
        }
        protected override void SetFields(d.Dataset objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_ProtocolDropdown.value = ApplicationState.ProjectLoaded.Protocols.IndexOf(objectToDisplay.Protocol);
            m_DataInfoListGestion.List.Set(objectToDisplay.Data);
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

        protected void OnChangeName(string value)
        {
            if (value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }
        protected void OnAddData(d.DataInfo data)
        {
            ItemTemp.AddData(data);
        }
        protected void OnRemoveData(d.DataInfo data)
        {
            ItemTemp.RemoveData(data);
        }
        protected void OnUpdateData(d.DataInfo data)
        {
            ItemTemp.UpdateData(data);
        }
        #endregion
    }
}