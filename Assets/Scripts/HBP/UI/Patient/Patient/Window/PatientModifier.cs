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

            preIRMFileSelector.File = objectToDisplay.Brain.PreOperationMRI;
            preIRMFileSelector.onValueChanged.RemoveAllListeners();
            preIRMFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PreOperationMRI = value);

            postIRMFileSelector.File = objectToDisplay.Brain.PostOperationMRI;
            postIRMFileSelector.onValueChanged.RemoveAllListeners();
            postIRMFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PostOperationMRI = value);

            patientBasedImplantantationFileSelector.File = objectToDisplay.Brain.PatientReferenceFrameImplantation;
            patientBasedImplantantationFileSelector.onValueChanged.RemoveAllListeners();
            patientBasedImplantantationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PatientReferenceFrameImplantation = value);

            MNIReferenceFrameImplantationFileSelector.File = objectToDisplay.Brain.MNIReferenceFrameImplantation;
            MNIReferenceFrameImplantationFileSelector.onValueChanged.RemoveAllListeners();
            MNIReferenceFrameImplantationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.MNIReferenceFrameImplantation = value);

            preToScannerBasedTransformationFileSelector.File = objectToDisplay.Brain.PreOperationReferenceFrameToScannerReferenceFrameTransformation;
            preToScannerBasedTransformationFileSelector.onValueChanged.RemoveAllListeners();
            preToScannerBasedTransformationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PreOperationReferenceFrameToScannerReferenceFrameTransformation = value);

            connectivityFileSelector.File = objectToDisplay.Brain.PlotsConnectivity;
            connectivityFileSelector.onValueChanged.RemoveAllListeners();
            connectivityFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PlotsConnectivity = value);

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
            nameInputField = transform.FindChild("Content").FindChild("General").FindChild("Name").GetComponentInChildren<InputField>();
            placeInputField = transform.FindChild("Content").FindChild("General").FindChild("Place").GetComponentInChildren<InputField>();
            dateInputField = transform.FindChild("Content").FindChild("General").FindChild("Date").GetComponentInChildren<InputField>();
            leftMeshFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("LeftCerebralHemisphereMesh").GetComponentInChildren<FileSelector>();
            rightMeshFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("RightCerebralHemisphereMesh").GetComponentInChildren<FileSelector>();
            preIRMFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PreOperationMRI").GetComponentInChildren<FileSelector>();
            postIRMFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PostOperationMRI").GetComponentInChildren<FileSelector>();
            patientBasedImplantantationFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PatientReferenceFrameImplantation").GetComponentInChildren<FileSelector>();
            MNIReferenceFrameImplantationFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("MNIReferenceFrameImplantation").GetComponentInChildren<FileSelector>();
            preToScannerBasedTransformationFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PreToScannerBasedTransformationFileSelector").GetComponentInChildren<FileSelector>();
            connectivityFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("Connectivity").GetComponentInChildren<FileSelector>();
            epilepsyTypeDropdown = transform.FindChild("Content").FindChild("Brain").FindChild("EpilepsyType").GetComponentInChildren<Dropdown>();
            saveButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Save").GetComponent<Button>();
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