using UnityEngine.UI;
using HBP.Data.Experience.Dataset;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoModifier : ObjectModifier<DataInfo>
    {
        #region Properties
        public new DataInfo ItemTemp { get => itemTemp; }

        List<DataInfo> m_DataInfoTemp;
        List<BaseSubModifier> m_SubModifiers;

        Type[] m_Types; 

        [SerializeField] InputField m_NameInputField;
        [SerializeField] PatientDataInfoSubModifier m_PatientDataInfoSubModifier;
        [SerializeField] Dropdown m_TypeDropdown;

        [SerializeField] DataContainerModifier m_DataContainerModifier;

        [SerializeField] iEEGDataInfoSubModifier m_iEEGDataInfoSubModifier;
        [SerializeField] CCEPDataInfoSubModifier m_CCEPDataInfoSubModifier;

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
                m_DataContainerModifier.Interactable = value;
            }
        }
        #endregion

        #region Private Methods
        public override void OK()
        {
            item = ItemTemp;
            base.OK();
        }
        protected override void SetFields(DataInfo objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));
        }
        protected override void Initialize()
        {
            base.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_iEEGDataInfoSubModifier,
                m_CCEPDataInfoSubModifier
            };

            m_DataInfoTemp = new List<DataInfo>
            {
                new IEEGDataInfo(),
                new CCEPDataInfo()
            };

            m_iEEGDataInfoSubModifier.Initialize();
            m_CCEPDataInfoSubModifier.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);

            m_TypeDropdown.onValueChanged.AddListener(ChangeDataInfoType);
            m_DataContainerModifier.OnChangeDataType.AddListener(OnChangeDataContainerType);
            m_Types = m_TypeDropdown.Set(typeof(DataInfo)); 
        }
        void ChangeDataInfoType(int value)
        {
            Type type = m_Types[value];

            m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(ItemTemp.GetType()))).IsActive = false;

            DataInfo dataInfo = m_DataInfoTemp.Find(d => d.GetType() == type);
            dataInfo.Copy(itemTemp);
            itemTemp = dataInfo;

            BaseSubModifier subModifier = m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            subModifier.IsActive = true;
            subModifier.Object = ItemTemp;

            if (type == typeof(IEEGDataInfo)) m_DataContainerModifier.DataAttribute = new iEEG();
            else if (type == typeof(CCEPDataInfo)) m_DataContainerModifier.DataAttribute = new CCEP();

            m_DataContainerModifier.Object = itemTemp.DataContainer;
            if (itemTemp is PatientDataInfo patientDataInfo) m_PatientDataInfoSubModifier.Object = patientDataInfo;
        }
        void OnChangeDataContainerType()
        {
            itemTemp.DataContainer = m_DataContainerModifier.Object;
        }
        protected void OnChangeName(string value)
        {
            if (value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }
        #endregion
    }
}