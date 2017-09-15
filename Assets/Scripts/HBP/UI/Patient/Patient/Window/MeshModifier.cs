using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data.Anatomy;
using System;
using Tools.Unity;

namespace HBP.UI.Anatomy
{
    public class MeshModifier : ItemModifier<Data.Anatomy.Mesh>
{
        #region Properties
        // General
        enum Type { Single, LeftRight}
        InputField m_NameInputField;
        Dropdown m_TypeDropdown;
        SingleMeshGestion m_SingleMeshGestion;
        LeftRightMeshGestion m_LeftRightMeshGestion;
        FileSelector m_TransformationFileSelector;

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        protected override void SetFields(Data.Anatomy.Mesh objectToDisplay)
        {
            // Name
            m_NameInputField.text = objectToDisplay.Name;

            // Type
            int value = 0;
            if (ItemTemp is LeftRightMesh) value = 1;
            m_TypeDropdown.value = value;
        }
        protected override void SetInteractableFields(bool interactable)
        {
        }
        protected override void SetWindow()
        {
            // Name
            m_NameInputField = transform.Find("Content").Find("General").Find("Name").Find("InputField").GetComponent<InputField>();

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
            m_TransformationFileSelector.File = ItemTemp.Transformation;
            m_TransformationFileSelector.onValueChanged.RemoveAllListeners();
            m_TransformationFileSelector.onValueChanged.AddListener((path) => ItemTemp.Transformation = path);
        }
        void ChangeMeshType(int i)
        {
            if(i == 0)
            {
                LeftRightMesh leftRightMesh = ItemTemp as LeftRightMesh;
                ItemTemp = new SingleMesh(leftRightMesh.Name, leftRightMesh.Transformation, leftRightMesh.LeftHemisphere, leftRightMesh.LeftMarsAtlasHemisphere);
                m_LeftRightMeshGestion.SetActive(false);
                m_SingleMeshGestion.Set(ItemTemp as SingleMesh);
                m_SingleMeshGestion.SetActive(true);

            }
            else if(i == 1)
            {
                SingleMesh singleMesh = ItemTemp as SingleMesh;
                ItemTemp = new LeftRightMesh(singleMesh.Name, singleMesh.Transformation, singleMesh.Path, string.Empty, singleMesh.MarsAtlasPath, string.Empty);
                m_SingleMeshGestion.SetActive(false);
                m_LeftRightMeshGestion.Set(ItemTemp as LeftRightMesh);
                m_LeftRightMeshGestion.SetActive(true);
            }
        }
        #endregion
    }
}