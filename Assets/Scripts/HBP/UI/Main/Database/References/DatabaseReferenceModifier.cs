using HBP.Data.Database;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabaseReferenceModifier : ObjectModifier<DatabaseReference>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] FolderSelector m_FolderSelector;

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
                m_TypeDropdown.interactable = value;
                m_FolderSelector.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_TypeDropdown.options = (from name in System.Enum.GetNames(typeof(DatabaseType)) select new Dropdown.OptionData(name, null)).ToList();
            m_TypeDropdown.onValueChanged.AddListener((value) => { ObjectTemp.Type = (DatabaseType)value; });
            m_FolderSelector.onValueChanged.AddListener(ChangePath);
        }
        protected override void SetFields(DatabaseReference objectToModify)
        {
            base.SetFields();

            m_NameInputField.text = objectToModify.Name;
            m_TypeDropdown.value = (int)objectToModify.Type;
            m_TypeDropdown.RefreshShownValue();
            m_FolderSelector.Folder = objectToModify.Path;
        }
        protected void ChangeName(string value)
        {
            if (value != "")
            {
                ObjectTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        protected void ChangePath(string value)
        {
            ObjectTemp.Path = value;
        }
        #endregion
    }
}