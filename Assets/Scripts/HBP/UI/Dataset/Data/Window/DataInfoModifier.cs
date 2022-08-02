using UnityEngine.UI;
using Tools.Unity;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// Window to modify a dataInfo.
    /// </summary>
    public class DataInfoModifier : ObjectModifier<Core.Data.DataInfo>
    {
        #region Properties
        public new Core.Data.DataInfo ObjectTemp => m_ObjectTemp;

        List<Core.Data.DataInfo> m_DataInfoTemp;
        List<BaseSubModifier> m_SubModifiers;

        Type[] m_Types; 

        [SerializeField] InputField m_NameInputField;
        [SerializeField] PatientDataInfoSubModifier m_PatientDataInfoSubModifier;
        [SerializeField] Dropdown m_TypeDropdown;

        [SerializeField] DataContainerModifier m_DataContainerModifier;

        [SerializeField] iEEGDataInfoSubModifier m_iEEGDataInfoSubModifier;
        [SerializeField] CCEPDataInfoSubModifier m_CCEPDataInfoSubModifier;
        [SerializeField] FMRIDataInfoSubModifier m_FMRIDataInfoSubModifier;
        [SerializeField] MEGvDataInfoSubModifier m_MEGvDataInfoSubModifier;
        [SerializeField] MEGcDataInfoSubModifier m_MEGcDataInfoSubModifier;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_TypeDropdown.interactable = value;
                m_PatientDataInfoSubModifier.Interactable = value;
                m_iEEGDataInfoSubModifier.Interactable = value;
                m_CCEPDataInfoSubModifier.Interactable = value;
                m_FMRIDataInfoSubModifier.Interactable = value;
                m_DataContainerModifier.Interactable = value;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            m_Object = ObjectTemp;
            base.OK();
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">DataInfo to modifiy</param>
        protected override void SetFields(Core.Data.DataInfo objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));
        }
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_iEEGDataInfoSubModifier,
                m_CCEPDataInfoSubModifier,
                m_FMRIDataInfoSubModifier,
                m_MEGvDataInfoSubModifier,
                m_MEGcDataInfoSubModifier
            };

            m_DataInfoTemp = new List<Core.Data.DataInfo>
            {
                new Core.Data.IEEGDataInfo(),
                new Core.Data.CCEPDataInfo(),
                new Core.Data.FMRIDataInfo(),
                new Core.Data.MEGvDataInfo(),
                new Core.Data.MEGcDataInfo()
            };

            m_iEEGDataInfoSubModifier.Initialize();
            m_CCEPDataInfoSubModifier.Initialize();
            m_FMRIDataInfoSubModifier.Initialize();
            m_MEGvDataInfoSubModifier.Initialize();
            m_MEGcDataInfoSubModifier.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);

            m_TypeDropdown.onValueChanged.AddListener(ChangeDataInfoType);
            m_DataContainerModifier.OnChangeDataType.AddListener(ChangeDataContainerType);
            m_Types = m_TypeDropdown.Set(typeof(Core.Data.DataInfo)); 
        }
        /// <summary>
        /// Change the type of the dataInfo.
        /// </summary>
        /// <param name="value">index of the type</param>
        void ChangeDataInfoType(int value)
        {
            Type type = m_Types[value];

            m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(ObjectTemp.GetType()))).IsActive = false;

            Core.Data.DataInfo dataInfo = m_DataInfoTemp.Find(d => d.GetType() == type);
            dataInfo.Copy(m_ObjectTemp);
            m_ObjectTemp = dataInfo;

            BaseSubModifier subModifier = m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            subModifier.IsActive = true;
            subModifier.Object = ObjectTemp;

            if (type == typeof(Core.Data.IEEGDataInfo)) m_DataContainerModifier.DataAttribute = new IEEG();
            else if (type == typeof(Core.Data.CCEPDataInfo)) m_DataContainerModifier.DataAttribute = new CCEP();
            else if (type == typeof(Core.Data.FMRIDataInfo)) m_DataContainerModifier.DataAttribute = new FMRI();
            else if (type == typeof(Core.Data.MEGvDataInfo)) m_DataContainerModifier.DataAttribute = new MEGv();
            else if (type == typeof(Core.Data.MEGcDataInfo)) m_DataContainerModifier.DataAttribute = new MEGc();

            m_DataContainerModifier.Object = m_ObjectTemp.DataContainer;
            if (m_ObjectTemp is Core.Data.PatientDataInfo patientDataInfo) m_PatientDataInfoSubModifier.Object = patientDataInfo;
        }
        /// <summary>
        /// Change the datacontainer type.
        /// </summary>
        void ChangeDataContainerType()
        {
            m_ObjectTemp.DataContainer = m_DataContainerModifier.Object;
        }
        /// <summary>
        /// Change the name of the dataInfo.
        /// </summary>
        /// <param name="name">Name</param>
        protected void ChangeName(string name)
        {
            if (name != "")
            {
                ObjectTemp.Name = name;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        #endregion
    }
}