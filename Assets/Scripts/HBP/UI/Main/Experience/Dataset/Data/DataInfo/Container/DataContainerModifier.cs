using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Data.Container;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class DataContainerModifier : SubModifier<DataContainer>
    {
        #region Properties
        [SerializeField] Dropdown m_ContainerTypeDropdown;
        [SerializeField] ElanDataContainerSubModifier m_ElanDataContainerSubModifier;
        [SerializeField] BrainVisionDataContainerSubModifier m_BrainVisionDataContainerSubModifier;
        [SerializeField] EDFDataContainerSubModifier m_EDFDataContainerSubModifier;
        [SerializeField] MicromedDataContainerSubModifier m_MicromedDataContainerSubModifier;
        [SerializeField] NiftiDataContainerSubModifier m_NiftiDataContainerSubModifier;
        [SerializeField] FIFDataContainerSubModifier m_FIFDataContainerSubModifier;

        Type[] m_Types;
        Elan m_ElanDataContainerTemp;
        EDF m_EDFDataContainerTemp;
        Micromed m_MicromedDataContainerTemp;
        BrainVision m_BrainVisionDataContainerTemp;
        Nifti m_NiftiDataContainerTemp;
        FIF m_FIFDataContainerTemp;

        DataAttribute m_DataAttribute;
        public DataAttribute DataAttribute
        {
            get
            {
                return m_DataAttribute;
            }
            set
            {
                m_DataAttribute = value;
                m_Types = m_ContainerTypeDropdown.Set(typeof(DataContainer), m_DataAttribute);
            }
        }

        public UnityEvent OnChangeDataType { get; } = new UnityEvent();

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_ContainerTypeDropdown.interactable = value;
                m_ElanDataContainerSubModifier.Interactable = value;
                m_EDFDataContainerSubModifier.Interactable = value;
                m_BrainVisionDataContainerSubModifier.Interactable = value;
                m_MicromedDataContainerSubModifier.Interactable = value;
                m_NiftiDataContainerSubModifier.Interactable = value;
                m_FIFDataContainerSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ContainerTypeDropdown.onValueChanged.AddListener(ChangeDataInfoType);
        }
        #endregion

        #region Private Methods
        void ChangeDataInfoType(int i)
        {
            Type type = m_Types[i];
            if (type == typeof(Elan))
            {
                m_ElanDataContainerSubModifier.IsActive = true;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;

                m_ElanDataContainerSubModifier.Object = m_ElanDataContainerTemp;
                m_Object = m_ElanDataContainerTemp;
            }
            else if (type == typeof(BrainVision))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = true;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;

                m_BrainVisionDataContainerSubModifier.Object = m_BrainVisionDataContainerTemp;
                m_Object = m_BrainVisionDataContainerTemp;
            }
            else if (type == typeof(EDF))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = true;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;

                m_EDFDataContainerSubModifier.Object = m_EDFDataContainerTemp;
                m_Object = m_EDFDataContainerTemp;
            }
            else if (type == typeof(Micromed))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = true;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;

                m_MicromedDataContainerSubModifier.Object = m_MicromedDataContainerTemp;
                m_Object = m_MicromedDataContainerTemp;
            }
            else if (type == typeof(Nifti))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = true;
                m_FIFDataContainerSubModifier.IsActive = false;

                m_NiftiDataContainerSubModifier.Object = m_NiftiDataContainerTemp;
                m_Object = m_NiftiDataContainerTemp;
            }
            else if (type == typeof(FIF))
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = true;

                m_FIFDataContainerSubModifier.Object = m_FIFDataContainerTemp;
                m_Object = m_FIFDataContainerTemp;
            }
            else
            {
                m_ElanDataContainerSubModifier.IsActive = false;
                m_BrainVisionDataContainerSubModifier.IsActive = false;
                m_EDFDataContainerSubModifier.IsActive = false;
                m_MicromedDataContainerSubModifier.IsActive = false;
                m_NiftiDataContainerSubModifier.IsActive = false;
                m_FIFDataContainerSubModifier.IsActive = false;
            }
            OnChangeDataType.Invoke();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(DataContainer objectToDisplay)
        {
            m_ElanDataContainerTemp = new Elan("", "", "", objectToDisplay.ID);
            m_EDFDataContainerTemp = new EDF("", objectToDisplay.ID);
            m_BrainVisionDataContainerTemp = new BrainVision("", objectToDisplay.ID);
            m_MicromedDataContainerTemp = new Micromed("", objectToDisplay.ID);
            m_NiftiDataContainerTemp = new Nifti("", objectToDisplay.ID);
            m_FIFDataContainerTemp = new FIF("", objectToDisplay.ID);

            if (objectToDisplay is Elan)
            {
                m_ElanDataContainerTemp = objectToDisplay as Elan;
            }
            else if (objectToDisplay is EDF)
            {
                m_EDFDataContainerTemp = objectToDisplay as EDF;
            }
            else if (objectToDisplay is Micromed)
            {
                m_MicromedDataContainerTemp = objectToDisplay as Micromed;
            }
            else if (objectToDisplay is BrainVision)
            {
                m_BrainVisionDataContainerTemp = objectToDisplay as BrainVision;
            }
            else if (objectToDisplay is Nifti)
            {
                m_NiftiDataContainerTemp = objectToDisplay as Nifti;
            }
            else if (objectToDisplay is FIF)
            {
                m_FIFDataContainerTemp = objectToDisplay as FIF;
            }
            m_ContainerTypeDropdown.SetValue(Array.IndexOf(m_Types, Object.GetType()));
        }
        #endregion
    }
}