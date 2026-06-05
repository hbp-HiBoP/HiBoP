using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Data.Preferences;
using HBP.Data.Database;
using HBP.UI.Database;

namespace HBP.UI.Main
{
    public class TagCollectionModifier : ObjectModifier<Core.Data.TagCollection>
    {
        #region Properties
        [SerializeField] GeneralTagsSubModifiers m_GeneralSubModifiers;
        [SerializeField] PatientsTagsSubModifiers m_PatientsSubModifiers;
        [SerializeField] SitesTagsSubModifiers m_SitesSubModifiers;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_GeneralSubModifiers.Interactable = value;
                m_PatientsSubModifiers.Interactable = value;
                m_SitesSubModifiers.Interactable = value;
            }
        }

        public ReadOnlyCollection<Core.Data.BaseTag> ModifiedTags
        {
            get
            {
                List<Core.Data.BaseTag> tags = new();
                tags.AddRange(m_GeneralSubModifiers.ModifiedTags);
                tags.AddRange(m_PatientsSubModifiers.ModifiedTags);
                tags.AddRange(m_SitesSubModifiers.ModifiedTags);
                return new ReadOnlyCollection<Core.Data.BaseTag>(tags);
            }
        }
        #endregion

        #region Public Methods
        public override async void OK()
        {
            if (m_GeneralSubModifiers.TagListGestion.HasBeenModified || m_PatientsSubModifiers.TagListGestion.HasBeenModified || m_SitesSubModifiers.TagListGestion.HasBeenModified)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Tags Modified", "Some tags have been added, deleted or modified. Patients and sites will be checked if you proceed.", "OK", "Cancel");
                if (result == 0)
                {
                    m_GeneralSubModifiers.Save();
                    m_PatientsSubModifiers.Save();
                    m_SitesSubModifiers.Save();
                    base.OK();
                    PersistentDataManager.Tags.Save();
                    var patients = GetPatientsToCheck();
                    var checkTagsTasks = CreateCheckPatientsTagsTasks(patients, ModifiedTags);
                    await LoadingManager.LoadAsync(update => RunCheckPatientsTagsTasksAsync(checkTagsTasks, update));
                    if (DatabaseManager.Database.IsLoaded) await DatabaseWorkflow.SaveDatabaseAsync();
                }
            }
            else
            {
                base.OK();
            }
        }
        #endregion

        #region Private Methods
        private List<Core.Data.Patient> GetPatientsToCheck()
        {
            List<Core.Data.Patient> patients = new();
            if (ApplicationState.LoadedProject != null) patients.AddRange(ApplicationState.LoadedProject.Patients);
            if (DatabaseManager.Database.IsLoaded) patients.AddRange(DatabaseManager.Database.Patients);
            return patients;
        }
        private List<Func<UniTask>> CreateCheckPatientsTagsTasks(IEnumerable<Core.Data.Patient> patients, IEnumerable<Core.Data.BaseTag> tags)
        {
            Core.Data.BaseTag[] tagsToCheck = tags.ToArray();
            return patients.Select(patient => (Func<UniTask>)(async () =>
            {
                await patient.CheckTagsAsync(tagsToCheck);
            })).ToList();
        }
        private async UniTask RunCheckPatientsTagsTasksAsync(IEnumerable<Func<UniTask>> tasks, Action<float, float, LoadingText> update)
        {
            await Core.Tools.UniTaskExtensions.PerformMultipleTasksAsync(tasks, 0, 1, "Checking patients", update, 20, PersistentDataManager.UserPreferences.General.System.MultiThreading);
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifiers.Initialize();
            m_PatientsSubModifiers.Initialize();
            m_SitesSubModifiers.Initialize();
        }
        protected override void SetFields(Core.Data.TagCollection objectToDisplay)
        {
            m_GeneralSubModifiers.Object = objectToDisplay;
            m_PatientsSubModifiers.Object = objectToDisplay;
            m_SitesSubModifiers.Object = objectToDisplay;
        }
        #endregion
    }
}
