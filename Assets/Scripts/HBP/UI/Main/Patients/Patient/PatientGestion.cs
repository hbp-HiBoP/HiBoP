using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Core.Database;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace HBP.UI.Main
{
    public class PatientGestion : GestionWindow<Patient>
    {
        #region Properties
        [SerializeField] PatientListGestion m_ListGestion;
        public override ListGestion<Patient> ListGestion => m_ListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_ListGestion.Interactable = value;
                m_ListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Public Methods
        public override async void OK()
        {
            if (DataManager.HasData)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", "Save & Reload", "Cancel");
                if (result == 0)
                {
                    base.OK();
                    await UniTask.SwitchToMainThread();
                    ApplicationState.LoadedProject.SetPatients(ListGestion.List.Objects);
                    DataManager.Clear();
                    var visualizations = Module3DMain.PrepareReloadScenes();
                    await LoadingManager.LoadAsync((update, token) => Module3DMain.LoadAsync(visualizations, update, token));
                    UITools.CheckProjectIDAndAskForRegeneration().Forget();
                }
            }
            else
            {
                base.OK();
                ApplicationState.LoadedProject.SetPatients(ListGestion.List.Objects);
                UITools.CheckProjectIDAndAskForRegeneration().Forget();
            }
            InteractableStateManager.SetInteractables();
        }
        public override void Close()
        {
            if (m_ListGestion.HasBeenModified)
                LoadingManager.Load(update => RestoreOldValuesAsync(ApplicationState.LoadedProject.Patients, update), false);
            base.Close();
        }
        public async void UpdateFromDatabase()
        {
            var result = await DialogBoxManager.OpenAsync(DialogBoxType.Informational, "Update Patients from Database", "This will overwrite every patients of this project with data from the database. Patients not found within the database will not be changed.\n\nDo you want to proceed ?", "Yes", "No");
            if (result == 0)
            {
                var updatedPatients = await LoadingManager.LoadAsync(UpdateFromDatabaseAsync);
                StringBuilder stringBuilder = new();
                if (updatedPatients.Count > 0)
                {
                    stringBuilder.AppendLine("<b>Updated patients:</b>");
                    foreach (var patient in updatedPatients)
                    {
                        stringBuilder.AppendLine(patient.ID);
                    }
                    stringBuilder.AppendLine();
                }
                if (stringBuilder.Length != 0)
                {
                    await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Informational, "Update Patients from Database", stringBuilder.ToString(), "OK");
                }
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            SetList(ApplicationState.LoadedProject.Patients);
        }
        private async UniTask<List<Patient>> UpdateFromDatabaseAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            var length = m_ListGestion.List.Objects.Count;
            var progress = 0;
            var updatedPatients = new List<Patient>();
            foreach (var patient in m_ListGestion.List.Objects)
            {
                token.ThrowIfCancellationRequested();
                updateProgress.Invoke((float)progress++ / length, 0, new LoadingText($"Importing {progress}/{length}"));
                Patient databasePatient = (Patient)DatabaseManager.Database.Patients.FirstOrDefault(p => p == patient)?.Clone();
                if (databasePatient != null)
                {
                    updatedPatients.Add(databasePatient);
                }
            }
            await UniTask.SwitchToMainThread();
            foreach (var patient in updatedPatients)
            {
                m_ListGestion.List.UpdateObject(patient);
            }
            return updatedPatients;
        }
        #endregion
    }
}
