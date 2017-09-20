using UnityEngine.UI;
using HBP.Data.Anatomy;
using System;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class MeshModifier : ItemModifier<Mesh>
{
        #region Properties
        // General
        enum Type { Single, LeftRight}
        InputField m_NameInputField;
        Dropdown m_TypeDropdown;
        SingleMeshGestion m_SingleMeshGestion;
        LeftRightMeshGestion m_LeftRightMeshGestion;
        FileSelector m_TransformationFileSelector;
        Button m_OKButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            Item = ItemTemp;
            SaveEvent.Invoke();
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetFields(Mesh objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_TypeDropdown.value = objectToDisplay is LeftRightMesh ? 1 : 0;
            m_TypeDropdown.onValueChanged.Invoke(m_TypeDropdown.value);
            m_TransformationFileSelector.File = objectToDisplay.Transformation;
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_TypeDropdown.interactable = interactable;
            m_SingleMeshGestion.interactable = interactable;
            m_LeftRightMeshGestion.interactable = interactable;
            m_TransformationFileSelector.interactable = interactable;
            m_OKButton.interactable = interactable;
        }
        protected override void SetWindow()
        {
            // Name
            m_NameInputField = transform.Find("Content").Find("General").Find("Name").Find("InputField").GetComponent<InputField>();
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            // Type
            m_TypeDropdown = transform.Find("Content").Find("General").Find("Type").Find("Dropdown").GetComponent<Dropdown>();
            string[] labels = Enum.GetNames(typeof(Type));
            m_TypeDropdown.ClearOptions();
            foreach (string label in labels)
            {
                m_TypeDropdown.options.Add(new Dropdown.OptionData(label));
            }
            m_TypeDropdown.RefreshShownValue();
            m_TypeDropdown.onValueChanged.AddListener(ChangeMeshType);

            // Gestion
            m_SingleMeshGestion = transform.Find("Content").Find("Files").Find("SingleMesh").GetComponent<SingleMeshGestion>();
            m_LeftRightMeshGestion = transform.Find("Content").Find("Files").Find("LeftRightMesh").GetComponent<LeftRightMeshGestion>();

            m_TransformationFileSelector = transform.Find("Content").Find("Files").Find("Transformation").Find("FileSelector").GetComponent<FileSelector>();
            m_TransformationFileSelector.onValueChanged.RemoveAllListeners();
            m_TransformationFileSelector.onValueChanged.AddListener((path) => ItemTemp.Transformation = path);

            m_OKButton = transform.Find("Content").Find("Buttons").Find("OK").GetComponent<Button>();
        }
        void ChangeMeshType(int i)
        {
            if(i == 0)
            {
                if(!(ItemTemp is SingleMesh))
                {
                    LeftRightMesh leftRightMesh = ItemTemp as LeftRightMesh;
                    itemTemp = new SingleMesh(leftRightMesh.Name, leftRightMesh.Transformation, leftRightMesh.ID, leftRightMesh.LeftHemisphere, leftRightMesh.LeftMarsAtlasHemisphere);
                }
                m_LeftRightMeshGestion.SetActive(false);
                m_SingleMeshGestion.Set(ItemTemp as SingleMesh);
                m_SingleMeshGestion.SetActive(true);
            }
            else if(i == 1)
            {
                if(!(ItemTemp is LeftRightMesh))
                {
                    SingleMesh singleMesh = ItemTemp as SingleMesh;
                    itemTemp = new LeftRightMesh(singleMesh.Name, singleMesh.Transformation, singleMesh.ID, singleMesh.Path, singleMesh.Path, singleMesh.MarsAtlasPath, singleMesh.MarsAtlasPath);
                }
                m_SingleMeshGestion.SetActive(false);
                m_LeftRightMeshGestion.Set(ItemTemp as LeftRightMesh);
                m_LeftRightMeshGestion.SetActive(true);
            }
        }
        #endregion
    }
}