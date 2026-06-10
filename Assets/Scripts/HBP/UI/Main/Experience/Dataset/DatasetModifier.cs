using HBP.Core.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using HBP.Core.Database;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify a dataset.
    /// </summary>
    public class DatasetModifier : ObjectModifier<Dataset>
    {
        #region Properties		
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_ProtocolDropdown;

        [SerializeField] DataInfoListGestion m_DataInfoListGestion;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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

        public Protocol SelectedProtocol => ObjectTemp.Protocol;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);

            m_ProtocolDropdown.options = (from protocol in DatabaseManager.Database.Protocols select new Dropdown.OptionData(protocol.Name)).ToList();
            m_ProtocolDropdown.onValueChanged.AddListener(ChangeProtocol);

            m_DataInfoListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_DataInfoListGestion.List.OnAddObject.AddListener(AddData);
            m_DataInfoListGestion.List.OnRemoveObject.AddListener(RemoveData);
            m_DataInfoListGestion.List.OnUpdateObject.AddListener(UpdateData);
            m_DataInfoListGestion.ObjectCreator.DatabaseFilterMethod = data => data.Protocol == ObjectTemp.Protocol;
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Dataset to modify</param>
        protected override void SetFields(Dataset objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_ProtocolDropdown.value = DatabaseManager.Database.Protocols.IndexOf(objectToDisplay.Protocol);
            m_DataInfoListGestion.List.Set(objectToDisplay.Data);
        }
        /// <summary>
        /// Change the porotocol.
        /// </summary>
        /// <param name="index">Index of the protocol</param>
        protected virtual void ChangeProtocol(int index)
        {
            ObjectTemp.Protocol = DatabaseManager.Database.Protocols[index];
            LoadingManager.Load(update => m_DataInfoListGestion.UpdateAllObjectsAsync(update));
        }
        /// <summary>
        /// Change the name.
        /// </summary>
        /// <param name="name">Name of the dataset</param>
        protected void ChangeName(string name)
        {
            if (name != "")
            {
                ObjectTemp.Name = name;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        /// <summary>
        /// Add data to the dataset.
        /// </summary>
        /// <param name="data">dataInfo to add</param>
        protected void AddData(DataInfo data)
        {
            ObjectTemp.AddData(data);
        }
        /// <summary>
        /// Remove data from the dataset.
        /// </summary>
        /// <param name="data">dataInfo to remove</param>
        protected void RemoveData(DataInfo data)
        {
            ObjectTemp.RemoveData(data);
        }
        /// <summary>
        /// Update data of the dataset.
        /// </summary>
        /// <param name="data">dataInfo to update</param>
        protected void UpdateData(DataInfo data)
        {
            ObjectTemp.UpdateData(data);
        }
        #endregion
    }
}