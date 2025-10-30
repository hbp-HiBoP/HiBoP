using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Database
{
    public class TrialMatrixActionsContextMenu : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrixDisplayer m_TrialMatrixDisplayer;
        #endregion

        #region Private Methods
        private void Awake()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region Public Methods
        public async void AddCurrentPatientToProjectGroup()
        {
            gameObject.SetActive(false);

            var patient = m_TrialMatrixDisplayer.CurrentPatient;

            if (patient == null)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No patient selected", "Please select a patient before adding them to a group.").Forget();
                return;
            }

            if (ApplicationState.LoadedProject == null)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No project loaded", "Please load a project before adding patients to a group.").Forget();
                return;
            }

            if (!ApplicationState.LoadedProject.Patients.Contains(patient))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Patient not in project", "The selected patient is not part of the loaded project. Please add the patient to the project first.").Forget();
                return;
            }

            if (ApplicationState.LoadedProject.Groups.Count == 0)
            {
                var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "No group available", "A new group will be created. Do you want to proceed?", "Yes", "No");
                if (result == 1) return; // User chose "No"

                var groupModifier = WindowsManager.OpenModifier(new Group("New group", new List<Patient>() { patient }), GetComponentInParent<DialogWindow>());
                groupModifier.OnOk.AddListener(() =>
                {
                    ApplicationState.LoadedProject.AddGroup(groupModifier.Object);
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Group created", $"Patient {patient.Name} has been added to the new group {groupModifier.Object.Name}.").Forget();
                });
            }
            else
            {
                var groupSelector = WindowsManager.OpenSelector(ApplicationState.LoadedProject.Groups, GetComponentInParent<DialogWindow>(), false);
                groupSelector.OnOk.AddListener(() =>
                {
                    if (groupSelector.ObjectsSelected.Length == 0)
                    {
                        DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No group selected", "Please select a group to add the patient to.").Forget();
                        return;
                    }
                    var selectedGroup = groupSelector.ObjectsSelected[0];
                    selectedGroup?.Patients.Add(patient);
                    DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Patient added", $"Patient {patient.Name} has been added to group {selectedGroup.Name}.").Forget();
                });
            }
        }
        #endregion
    }
}