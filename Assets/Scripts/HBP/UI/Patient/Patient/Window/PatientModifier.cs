using UnityEngine.UI;
using Tools.Unity;
using System;
using System.Collections.Generic;
using HBP.Data.Anatomy;


namespace HBP.UI.Patient
{
    public class PatientModifier : ItemModifier<Data.Patient>
    {
        #region Properties
        InputField nameInputField, placeInputField, dateInputField;
        FileSelector leftMeshFileSelector, rightMeshFileSelector, preIRMFileSelector, postIRMFileSelector, patientBasedImplantantationFileSelector, MNIReferenceFrameImplantationFileSelector, preToScannerBasedTransformationFileSelector,connectivityFileSelector;
        Dropdown epilepsyTypeDropdown;
        Button saveButton;
        #endregion

        #region Protected Methods
        protected override void SetFields(Data.Patient objectToDisplay)
        {
            // General.
            nameInputField.text = objectToDisplay.Name;
            nameInputField.onValueChanged.RemoveAllListeners();
            nameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);

            placeInputField.text = objectToDisplay.Place;
            placeInputField.onValueChanged.RemoveAllListeners();
            placeInputField.onValueChanged.AddListener((value) => ItemTemp.Place = value);

            dateInputField.text = objectToDisplay.Date.ToString();
            dateInputField.onValueChanged.RemoveAllListeners();
            dateInputField.onValueChanged.AddListener((value) => ItemTemp.Date = int.Parse(value));

            // Brain.
            leftMeshFileSelector.File = objectToDisplay.Brain.LeftCerebralHemisphereMesh;
            leftMeshFileSelector.onValueChanged.RemoveAllListeners();
            leftMeshFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.LeftCerebralHemisphereMesh = value);

            rightMeshFileSelector.File = objectToDisplay.Brain.RightCerebralHemisphereMesh;
            rightMeshFileSelector.onValueChanged.RemoveAllListeners();
            rightMeshFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.RightCerebralHemisphereMesh = value);

            preIRMFileSelector.File = objectToDisplay.Brain.PreoperativeMRI;
            preIRMFileSelector.onValueChanged.RemoveAllListeners();
            preIRMFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PreoperativeMRI = value);

            postIRMFileSelector.File = objectToDisplay.Brain.PostoperativeMRI;
            postIRMFileSelector.onValueChanged.RemoveAllListeners();
            postIRMFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PostoperativeMRI = value);

            patientBasedImplantantationFileSelector.File = objectToDisplay.Brain.PatientBasedImplantation;
            patientBasedImplantantationFileSelector.onValueChanged.RemoveAllListeners();
            patientBasedImplantantationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PatientBasedImplantation = value);

            MNIReferenceFrameImplantationFileSelector.File = objectToDisplay.Brain.MNIBasedImplantation;
            MNIReferenceFrameImplantationFileSelector.onValueChanged.RemoveAllListeners();
            MNIReferenceFrameImplantationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.MNIBasedImplantation = value);

            preToScannerBasedTransformationFileSelector.File = objectToDisplay.Brain.PreoperativeBasedToScannerBasedTransformation;
            preToScannerBasedTransformationFileSelector.onValueChanged.RemoveAllListeners();
            preToScannerBasedTransformationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PreoperativeBasedToScannerBasedTransformation = value);

            connectivityFileSelector.File = objectToDisplay.Brain.SitesConnectivities;
            connectivityFileSelector.onValueChanged.RemoveAllListeners();
            connectivityFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.SitesConnectivities = value);

            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (int value in Enum.GetValues(typeof(Epilepsy.EpilepsyType)))
            {
                options.Add(new Dropdown.OptionData(Epilepsy.GetFullEpilepsyName((Epilepsy.EpilepsyType) value)));
            }
            epilepsyTypeDropdown.options = options;
            epilepsyTypeDropdown.value = (int) ItemTemp.Brain.Epilepsy.Type;
            epilepsyTypeDropdown.onValueChanged.RemoveAllListeners();
            epilepsyTypeDropdown.onValueChanged.AddListener((value) => ItemTemp.Brain.Epilepsy.Type = (Epilepsy.EpilepsyType) value);
        }
        protected override void SetWindow()
        {
            nameInputField = transform.Find("Content").Find("General").Find("Name").GetComponentInChildren<InputField>();
            placeInputField = transform.Find("Content").Find("General").Find("Place").GetComponentInChildren<InputField>();
            dateInputField = transform.Find("Content").Find("General").Find("Date").GetComponentInChildren<InputField>();
            leftMeshFileSelector = transform.Find("Content").Find("Brain").Find("LeftCerebralHemisphereMesh").GetComponentInChildren<FileSelector>();
            rightMeshFileSelector = transform.Find("Content").Find("Brain").Find("RightCerebralHemisphereMesh").GetComponentInChildren<FileSelector>();
            preIRMFileSelector = transform.Find("Content").Find("Brain").Find("PreOperationMRI").GetComponentInChildren<FileSelector>();
            postIRMFileSelector = transform.Find("Content").Find("Brain").Find("PostOperationMRI").GetComponentInChildren<FileSelector>();
            patientBasedImplantantationFileSelector = transform.Find("Content").Find("Brain").Find("PatientReferenceFrameImplantation").GetComponentInChildren<FileSelector>();
            MNIReferenceFrameImplantationFileSelector = transform.Find("Content").Find("Brain").Find("MNIReferenceFrameImplantation").GetComponentInChildren<FileSelector>();
            preToScannerBasedTransformationFileSelector = transform.Find("Content").Find("Brain").Find("PreToScannerBasedTransformationFileSelector").GetComponentInChildren<FileSelector>();
            connectivityFileSelector = transform.Find("Content").Find("Brain").Find("Connectivity").GetComponentInChildren<FileSelector>();
            epilepsyTypeDropdown = transform.Find("Content").Find("Brain").Find("EpilepsyType").GetComponentInChildren<Dropdown>();
            saveButton = transform.Find("Content").Find("Buttons").Find("Save").GetComponent<Button>();
        }
        protected override void SetInteractableFields(bool interactable)
        {
            // InputField.
            nameInputField.interactable = interactable;
            placeInputField.interactable = interactable;
            dateInputField.interactable = interactable;

            // InputFile.
            leftMeshFileSelector.interactable = interactable;
            rightMeshFileSelector.interactable = interactable;
            preIRMFileSelector.interactable = interactable;
            postIRMFileSelector.interactable = interactable;
            patientBasedImplantantationFileSelector.interactable = interactable;
            MNIReferenceFrameImplantationFileSelector.interactable = interactable;
            preToScannerBasedTransformationFileSelector.interactable = interactable;
            connectivityFileSelector.interactable = interactable;
            epilepsyTypeDropdown.interactable = interactable;
            // Buttons.
            saveButton.interactable = interactable;
        }
        #endregion
    }
}