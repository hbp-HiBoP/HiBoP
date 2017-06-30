using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data;

namespace HBP.UI.Anatomy
{
	/// <summary>
	/// Display/Modify group.
	/// </summary>
	public class GroupModifier : ItemModifier<Group> 
	{
        #region Properties
        [SerializeField]
        GameObject patientModifierPrefab;

        InputField nameInputField;
        Button saveButton, addButton, removeButton;
        PatientList groupPatientsList, projectPatientsList;
		#endregion

		#region Public Methods
		public void AddPatients()
		{
            Data.Patient[] patients = projectPatientsList.GetObjectsSelected();
            ItemTemp.AddPatient(patients);
            projectPatientsList.DeactivateObject(patients);
            groupPatientsList.Add(patients);
        }
        public void RemovePatients()
		{
            Data.Patient[] patients = groupPatientsList.GetObjectsSelected();
            ItemTemp.RemovePatient(patients);
            groupPatientsList.Remove(patients);
            projectPatientsList.ActiveObject(patients);
        }
        public void OpenPatientModifier(Data.Patient patientToModify)
        {
            RectTransform obj = Instantiate(patientModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            PatientModifier patientModifier = obj.GetComponent<PatientModifier>();
            patientModifier.Open(patientToModify, false);
            patientModifier.CloseEvent.AddListener(() => OnClosePatientModifier());
            SetInteractable(false);
        }
        #endregion

        #region Private Methods
        protected void OnClosePatientModifier()
        {
            SetInteractable(true);
        }
        protected override void SetFields(Group objectToDisplay)
        {
            nameInputField.text = ItemTemp.Name;
            projectPatientsList.Display(ApplicationState.ProjectLoaded.Patients.ToArray(), ItemTemp.Patients.ToArray());
            groupPatientsList.Display(ItemTemp.Patients.ToArray());
            nameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            groupPatientsList.ActionEvent.AddListener((patient, i) => OpenPatientModifier(patient));
            projectPatientsList.ActionEvent.AddListener((patient, i) => OpenPatientModifier(patient));
        }
        protected override void SetWindow()
        {
            nameInputField = transform.Find("Content").Find("Name").Find("InputField").GetComponent<InputField>();
            projectPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("ProjectPatients").Find("List").Find("Viewport").Find("Content").GetComponent<PatientList>();
            groupPatientsList = transform.Find("Content").Find("Patients").Find("Lists").Find("GroupPatients").Find("List").Find("Viewport").Find("Content").GetComponent<PatientList>();
            saveButton = transform.Find("Content").Find("Buttons").Find("Save").GetComponent<Button>();
            addButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Add").GetComponent<Button>();
            removeButton = transform.Find("Content").Find("Patients").Find("Lists").Find("Buttons").Find("Remove").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            nameInputField.interactable = interactable;
            saveButton.interactable = interactable;
            removeButton.interactable = interactable;
            addButton.interactable = interactable;
        }
        #endregion
    }
}