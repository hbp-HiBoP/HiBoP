﻿using UnityEngine.UI;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class PatientModifier : ItemModifier<Data.Patient>
    {
        #region Properties
        // General.
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_PlaceInputField;
        [SerializeField] InputField m_DateInputField;

        // Meshes.
        [SerializeField] MeshListGestion m_MeshListGestion;
        [SerializeField] Button m_AddMeshButton, m_RemoveMeshButton;

        // MRI.
        [SerializeField] MRIListGestion m_MRIListGestion;
        [SerializeField] Button m_AddMRIButton, m_RemoveMRIButton;

        // Implantation.
        [SerializeField] ImplantationListGestion m_ImplantationListGestion;
        [SerializeField] Button m_AddImplantationButton, m_RemoveImplantationButton;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                // General.
                m_NameInputField.interactable = value;
                m_PlaceInputField.interactable = value;
                m_DateInputField.interactable = value;

                // Mesh.
                m_MeshListGestion.Interactable = value;
                m_AddMeshButton.interactable = value;
                m_RemoveMeshButton.interactable = value;

                // MRI.
                m_MRIListGestion.Interactable = value;
                m_AddMRIButton.interactable = value;
                m_RemoveMRIButton.interactable = value;

                // Implantation.
                m_ImplantationListGestion.Interactable = value;
                m_AddImplantationButton.interactable = value;
                m_RemoveImplantationButton.interactable = value;

                // Tags.
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_MeshListGestion.Initialize(m_SubWindows);
            m_ImplantationListGestion.Initialize(m_SubWindows);
            m_MRIListGestion.Initialize(m_SubWindows);

            m_NameInputField.onValueChanged.AddListener((value) => ItemTemp.Name = value);
            m_PlaceInputField.onValueChanged.AddListener((value) => ItemTemp.Place = value);
            m_DateInputField.onValueChanged.AddListener((value) => ItemTemp.Date = int.Parse(value));

        }
        protected override void SetFields(Data.Patient objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_PlaceInputField.text = objectToDisplay.Place;
            m_DateInputField.text = objectToDisplay.Date.ToString();


            m_MeshListGestion.Objects = objectToDisplay.Meshes;
            m_ImplantationListGestion.Objects = objectToDisplay.Implantations;
            m_MRIListGestion.Objects = objectToDisplay.MRIs;
        }
        #endregion
    }
}