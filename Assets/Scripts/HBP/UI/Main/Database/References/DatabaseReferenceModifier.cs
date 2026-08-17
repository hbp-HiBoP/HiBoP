using HBP.Core.Tools;
using HBP.Core.Database;
using HBP.UI.Tools;
using System.Collections.Generic;
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
            get { return base.Interactable; }
            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_TypeDropdown.interactable = value;
                m_FolderSelector.interactable = value;
            }
        }

        [SerializeField] BrainvisaDatabaseParametersSubModifier m_BrainvisaDatabaseParametersSubModifier;
        [SerializeField] LocalizerDatabaseParametersSubModifier m_LocalizerDatabaseParametersSubModifier;
        [SerializeField] BIDSDatabaseParametersSubModifier m_BIDSDatabaseParametersSubModifier;
        [SerializeField] TagsDatabaseParametersSubModifier m_TagsDatabaseParametersSubModifier;

        Dictionary<DatabaseType, BaseSubModifier> m_SubModifiers;
        Dictionary<DatabaseType, DatabaseReferenceParameters> m_ParametersTemp;

        #endregion

        #region Private Methods

        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_FolderSelector.onValueChanged.AddListener(OnChangePath);

            m_BrainvisaDatabaseParametersSubModifier.Initialize();
            m_LocalizerDatabaseParametersSubModifier.Initialize();
            m_BIDSDatabaseParametersSubModifier.Initialize();
            m_TagsDatabaseParametersSubModifier.Initialize();

            m_SubModifiers = new Dictionary<DatabaseType, BaseSubModifier>
            {
                { DatabaseType.Brainvisa, m_BrainvisaDatabaseParametersSubModifier },
                { DatabaseType.Localizer, m_LocalizerDatabaseParametersSubModifier },
                { DatabaseType.BIDS, m_BIDSDatabaseParametersSubModifier },
                { DatabaseType.Tags, m_TagsDatabaseParametersSubModifier }
            };

            m_ParametersTemp = new Dictionary<DatabaseType, DatabaseReferenceParameters>
            {
                { DatabaseType.Brainvisa, new BrainvisaDatabaseParameters() },
                { DatabaseType.Localizer, new LocalizerDatabaseParameters() },
                { DatabaseType.BIDS, new BIDSDatabaseParameters() },
                { DatabaseType.Tags, new TagsDatabaseParameters() }
            };
        }

        protected override void SetFields(DatabaseReference objectToModify)
        {
            base.SetFields();

            m_NameInputField.text = objectToModify.Name;
            m_TypeDropdown.Set(typeof(DatabaseType), (int)objectToModify.Type);
            m_FolderSelector.Folder = objectToModify.Path;
        }

        protected void OnChangeName(string value)
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

        protected void OnChangePath(string value)
        {
            ObjectTemp.Path = value;
        }

        protected void OnChangeType(int value)
        {
            ObjectTemp.Type = (DatabaseType)value;

            foreach (var sm in m_SubModifiers.Values)
                sm.IsActive = false;

            DatabaseReferenceParameters parameters = m_ParametersTemp[ObjectTemp.Type];
            parameters.Copy(ObjectTemp.Parameters);
            ObjectTemp.Parameters = parameters;

            BaseSubModifier subModifier = m_SubModifiers[ObjectTemp.Type];
            subModifier.IsActive = true;
            subModifier.Object = ObjectTemp.Parameters;
        }

        #endregion
    }
}
