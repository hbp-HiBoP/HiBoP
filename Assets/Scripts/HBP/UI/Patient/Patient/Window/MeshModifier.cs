﻿using UnityEngine.UI;
using HBP.Data.Anatomy;
using System;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class MeshModifier : ItemModifier<Data.Anatomy.Mesh>
{
        #region Properties
        enum Type { Single, LeftRight}
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] SingleMeshGestion m_SingleMeshGestion;
        [SerializeField] LeftRightMeshGestion m_LeftRightMeshGestion;
        [SerializeField] FileSelector m_TransformationFileSelector;
        [SerializeField] Button m_OKButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            if (m_TypeDropdown.value == (int)Type.Single)
            {
                SingleMesh mesh = (SingleMesh)ItemTemp;
                Item = new SingleMesh(mesh.Name, mesh.Transformation, mesh.ID, mesh.Path, mesh.MarsAtlasPath);
            }
            else if (m_TypeDropdown.value == (int)Type.LeftRight)
            {
                LeftRightMesh mesh = (LeftRightMesh)ItemTemp;
                Item = new LeftRightMesh(mesh.Name, mesh.Transformation, mesh.ID, mesh.LeftHemisphere, mesh.RightHemisphere, mesh.LeftMarsAtlasHemisphere, mesh.RightMarsAtlasHemisphere);
            }
            Item.RecalculateUsable();
            SaveEvent.Invoke();
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetFields(Data.Anatomy.Mesh objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_TypeDropdown.value = objectToDisplay is LeftRightMesh ? 1 : 0;
            m_TypeDropdown.onValueChanged.Invoke(m_TypeDropdown.value);
            m_TransformationFileSelector.File = objectToDisplay.SavedTransformation;
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
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            string[] labels = Enum.GetNames(typeof(Type));
            m_TypeDropdown.ClearOptions();
            foreach (string label in labels)
            {
                m_TypeDropdown.options.Add(new Dropdown.OptionData(label));
            }
            m_TypeDropdown.RefreshShownValue();
            m_TypeDropdown.onValueChanged.AddListener(ChangeMeshType);

            m_TransformationFileSelector.onValueChanged.RemoveAllListeners();
            m_TransformationFileSelector.onValueChanged.AddListener((path) => ItemTemp.Transformation = path);
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
                m_LeftRightMeshGestion.Set((LeftRightMesh) ItemTemp);
                m_LeftRightMeshGestion.SetActive(true);
            }
        }
        #endregion
    }
}