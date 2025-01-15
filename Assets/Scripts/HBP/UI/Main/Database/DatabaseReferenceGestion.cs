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
                SetUpdateButtonInteractableState();
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();
            DatabaseManager.Database.SetDatabaseReferences(m_ListGestion.List.Objects);
            DatabaseManager.Database.SaveDatabaseReferences();
            InteractableStateManager.SetInteractables();
        }
        public void UpdateDatabases()
        {
            DatabaseManager.Database.UpdateDatabases(m_ListGestion.List.ObjectsSelected, m_ListGestion.List.Refresh);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_ListGestion.List.OnSelect.AddListener((database) => SetUpdateButtonInteractableState());
            m_ListGestion.List.OnDeselect.AddListener((database) => SetUpdateButtonInteractableState());
        }
        private void SetUpdateButtonInteractableState()
        {
            var selectedDatabases = m_ListGestion.List.ObjectsSelected;
            m_UpdateButton.interactable = selectedDatabases.Length > 0 && Interactable;
        }
        protected override void SetFields()
        {
            base.SetFields();
            ListGestion.List.Set(DatabaseManager.Database.DatabaseReferences);
            SetUpdateButtonInteractableState();
        }
        #endregion
    }
}