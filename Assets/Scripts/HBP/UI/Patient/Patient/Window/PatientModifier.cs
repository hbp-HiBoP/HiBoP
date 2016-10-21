using p = HBP.Data.Patient;
using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;

namespace HBP.UI.Patient
{
    public class PatientModifier : ItemModifier<p.Patient>
    {
        #region Properties
        InputField nameInputField, placeInputField, dateInputField;
        FileSelector leftMeshFileSelector, rightMeshFileSelector, preIRMFileSelector, postIRMFileSelector, patientBasedImplantantationFileSelector, MNIBasedImplantationFileSelector, preToScannerBasedTransformationFileSelector;
        Button saveButton, addConnectivityButton, removeConnectivityButton;
        ConnectivityList connectivityList;
        Toggle selectAllConnectivityToggle;
        #endregion

        #region Public methods
        public void AddConnectivity()
        {
            ItemTemp.Brain.Connectivities.Add(new p.Connectivity());
            connectivityList.Display(ItemTemp.Brain.Connectivities.ToArray());
        }
        public void RemoveConnectivity()
        {
            p.Connectivity[] l_connectivityToRemove = connectivityList.GetObjectsSelected();
            foreach(p.Connectivity i_connectivity in l_connectivityToRemove)
            {
                ItemTemp.Brain.Connectivities.Remove(i_connectivity);
            }
            connectivityList.Display(ItemTemp.Brain.Connectivities.ToArray());
        }
        public override void Save()
        {
            connectivityList.SaveAll();
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(p.Patient objectToDisplay)
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
            leftMeshFileSelector.File = objectToDisplay.Brain.LeftMesh;
            leftMeshFileSelector.onValueChanged.RemoveAllListeners();
            leftMeshFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.LeftMesh = value);

            rightMeshFileSelector.File = objectToDisplay.Brain.RightMesh;
            rightMeshFileSelector.onValueChanged.RemoveAllListeners();
            rightMeshFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.RightMesh = value);

            preIRMFileSelector.File = objectToDisplay.Brain.PreIRM;
            preIRMFileSelector.onValueChanged.RemoveAllListeners();
            preIRMFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PreIRM = value);

            postIRMFileSelector.File = objectToDisplay.Brain.PostIRM;
            postIRMFileSelector.onValueChanged.RemoveAllListeners();
            postIRMFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PostIRM = value);

            patientBasedImplantantationFileSelector.File = objectToDisplay.Brain.PatientBasedImplantation;
            patientBasedImplantantationFileSelector.onValueChanged.RemoveAllListeners();
            patientBasedImplantantationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PatientBasedImplantation = value);

            MNIBasedImplantationFileSelector.File = objectToDisplay.Brain.MNIBasedImplantation;
            MNIBasedImplantationFileSelector.onValueChanged.RemoveAllListeners();
            MNIBasedImplantationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.MNIBasedImplantation = value);

            preToScannerBasedTransformationFileSelector.File = objectToDisplay.Brain.PreToScannerBasedTransformation;
            preToScannerBasedTransformationFileSelector.onValueChanged.RemoveAllListeners();
            preToScannerBasedTransformationFileSelector.onValueChanged.AddListener((value) => ItemTemp.Brain.PreToScannerBasedTransformation = value);

            // List.
            connectivityList.Display(objectToDisplay.Brain.Connectivities.ToArray());
        }
        protected override void SetWindow()
        {
            nameInputField = transform.FindChild("Content").FindChild("General").FindChild("Name").GetComponentInChildren<InputField>();
            placeInputField = transform.FindChild("Content").FindChild("General").FindChild("Place").GetComponentInChildren<InputField>();
            dateInputField = transform.FindChild("Content").FindChild("General").FindChild("Date").GetComponentInChildren<InputField>();
            leftMeshFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("LeftMesh").GetComponentInChildren<FileSelector>();
            rightMeshFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("RightMesh").GetComponentInChildren<FileSelector>();
            preIRMFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PreIRM").GetComponentInChildren<FileSelector>();
            postIRMFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PostIRM").GetComponentInChildren<FileSelector>();
            patientBasedImplantantationFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PatientBasedImplantation").GetComponentInChildren<FileSelector>();
            MNIBasedImplantationFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("MNIBasedImplantation").GetComponentInChildren<FileSelector>();
            preToScannerBasedTransformationFileSelector = transform.FindChild("Content").FindChild("Brain").FindChild("PreToScannerBasedTransformationFileSelector").GetComponentInChildren<FileSelector>();
            connectivityList = transform.FindChild("Content").FindChild("Connectivity").FindChild("List").FindChild("Viewport").Find("Content").GetComponent<ConnectivityList>();
            selectAllConnectivityToggle = transform.FindChild("Content").FindChild("Connectivity").FindChild("SelectAll").GetComponent<Toggle>();
            saveButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Save").GetComponent<Button>();
            addConnectivityButton = transform.FindChild("Content").FindChild("Connectivity").FindChild("Buttons").FindChild("Add").GetComponent<Button>();
            removeConnectivityButton = transform.FindChild("Content").FindChild("Connectivity").FindChild("Buttons").FindChild("Remove").GetComponent<Button>();
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
            MNIBasedImplantationFileSelector.interactable = interactable;
            preToScannerBasedTransformationFileSelector.interactable = interactable;

            // Buttons.
            saveButton.interactable = interactable;
            addConnectivityButton.interactable = interactable;
            removeConnectivityButton.interactable = interactable;

            // Toggle.
            selectAllConnectivityToggle.interactable = interactable;
        }
        #endregion
    }
}