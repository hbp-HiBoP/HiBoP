using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabaseReferenceGestion : GestionWindow<DatabaseReference>
    {
        #region Properties
        [SerializeField] Button m_UpdateButton;
        [SerializeField] DatabaseReferenceListGestion m_ListGestion;
        public override ListGestion<DatabaseReference> ListGestion => m_ListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_ListGestion.Interactable = value;
                m_ListGestion.Modifiable = value;
                SetUpdateButtonInteractableState();
            }
        }
        #endregion

        #region Public Methods
        public async override void OK()
        {
            if (m_ListGestion.HasBeenModified)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Save references", "Patients and data of removed references will be deleted. Do you want to continue?", "Yes", "Cancel");
                if (result == 0)
                {
                    base.OK();
                    DatabaseManager.Database.SetDatabaseReferences(m_ListGestion.List.Objects);
                    DatabaseManager.Database.SaveDatabaseReferences().Forget();
                    InteractableStateManager.SetInteractables();
                }
            }
            else
            {
                base.OK();
                InteractableStateManager.SetInteractables();
            }
        }
        public async void UpdateDatabases()
        {
            int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Override data", "Patients and data will be overridden. Do you want to continue?", "Yes", "Cancel");
            if (result == 0)
            {
                try
                {
                    DatabaseManager.Database.SetDatabaseReferences(m_ListGestion.List.Objects);
                    await DatabaseManager.Database.UpdateDatabases(m_ListGestion.List.ObjectsSelected);
                    m_ListGestion.HasBeenModified = true;
                    base.OK();
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        public override void Close()
        {
            if (m_ListGestion.HasBeenModified)
                LoadingManager.Load(update => RestoreOldValuesAsync(DatabaseManager.Database.DatabaseReferences, update), false);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_ListGestion.List.OnSelect.AddListener((database) => SetUpdateButtonInteractableState());
            m_ListGestion.List.OnDeselect.AddListener((database) => SetUpdateButtonInteractableState());
            m_ListGestion.List.OnRemoveObject.AddListener((database) => SetUpdateButtonInteractableState());
            m_ListGestion.List.OnAddObject.AddListener((database) => SetUpdateButtonInteractableState());
        }
        private void SetUpdateButtonInteractableState()
        {
            var selectedDatabases = m_ListGestion.List.ObjectsSelected;
            m_UpdateButton.interactable = selectedDatabases.Length > 0 && Interactable;
        }
        protected override void SetFields()
        {
            base.SetFields();
            SetList(DatabaseManager.Database.DatabaseReferences);
            SetUpdateButtonInteractableState();
        }
        #endregion
    }
}