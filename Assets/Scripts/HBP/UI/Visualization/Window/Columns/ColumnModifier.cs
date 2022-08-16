using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using HBP.Core.Tools;

namespace HBP.UI
{
    public class ColumnModifier : SubModifier<Core.Data.Column>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] IEEGColumnModifier m_IEEGColumnModifier;
        [SerializeField] CCEPColumnModifier m_CCEPColumnModifier;
        [SerializeField] FMRIColumnModifier m_FMRIColumnModifier;
        [SerializeField] AnatomicColumnModifier m_AnatomicColumnModifier;
        [SerializeField] MEGColumnModifier m_MEGColumnModifier;

        Type[] m_Types;

        public override Core.Data.Column Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                if (!Initialized) Initialize();
                base.Object = value;
                if(base.Object != null)
                {
                    m_NameInputField.interactable = m_Interactable;
                    m_NameInputField.text = value.Name;
                    m_TypeDropdown.interactable = m_Interactable;
                    m_TypeDropdown.SetValue(Array.IndexOf(m_Types, value.GetType()));
                }
                else
                {
                    m_NameInputField.interactable = false;
                    m_NameInputField.text = "";
                    m_TypeDropdown.interactable = false;
                    m_TypeDropdown.SetValue(Array.IndexOf(m_Types, typeof(Core.Data.AnatomicColumn)));
                }
                OnChangeColumn.Invoke(value);
            }
        }

        Core.Data.Patient[] m_Patients;
        public Core.Data.Patient[] Patients
        {
            get
            {
                return m_Patients;
            }
            set
            {
                if (!Initialized) Initialize();
                m_Patients = value;

                m_IEEGColumnModifier.Patients = value;
                m_CCEPColumnModifier.Patients = value;
                m_FMRIColumnModifier.Patients = value;
                m_MEGColumnModifier.Patients = value;
            }
        }

        public override bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_TypeDropdown.interactable = value;
                m_IEEGColumnModifier.Interactable = value;
                m_CCEPColumnModifier.Interactable = value;
                m_FMRIColumnModifier.Interactable = value;
                m_AnatomicColumnModifier.Interactable = value;
                m_MEGColumnModifier.Interactable = value;
            }
        }

        public GenericEvent<string> OnChangeName { get; } = new GenericEvent<string>();
        public GenericEvent<Core.Data.Column> OnChangeColumn { get; } = new GenericEvent<Core.Data.Column>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener(OnChangeNameHanlder);
            m_TypeDropdown.onValueChanged.AddListener(OnChangeTypeHandler);
            m_Types = m_TypeDropdown.Set(typeof(Core.Data.Column));
        }
        #endregion

        #region Private Methods
        void OnChangeTypeHandler(int value)
        {
            Type type = m_Types[value];
            if (m_Object != null)
            {
                if (type == typeof(Core.Data.AnatomicColumn))
                {
                    if (!(m_Object is Core.Data.AnatomicColumn)) Object = new Core.Data.AnatomicColumn(Object.Name, Object.BaseConfiguration);
                    m_AnatomicColumnModifier.Object = Object as Core.Data.AnatomicColumn;

                    m_AnatomicColumnModifier.IsActive = true;
                    m_IEEGColumnModifier.IsActive = false;
                    m_CCEPColumnModifier.IsActive = false;
                    m_FMRIColumnModifier.IsActive = false;
                    m_MEGColumnModifier.IsActive = false;
                }
                else if (type == typeof(Core.Data.IEEGColumn))
                {
                    if (!(m_Object is Core.Data.IEEGColumn)) Object = new Core.Data.IEEGColumn(Object.Name, Object.BaseConfiguration, m_Patients);
                    m_IEEGColumnModifier.Object = Object as Core.Data.IEEGColumn;

                    m_AnatomicColumnModifier.IsActive = false;
                    m_IEEGColumnModifier.IsActive = true;
                    m_CCEPColumnModifier.IsActive = false;
                    m_FMRIColumnModifier.IsActive = false;
                    m_MEGColumnModifier.IsActive = false;
                }
                else if (type == typeof(Core.Data.CCEPColumn))
                {
                    if (!(m_Object is Core.Data.CCEPColumn)) Object = new Core.Data.CCEPColumn(Object.Name, Object.BaseConfiguration, m_Patients);
                    m_CCEPColumnModifier.Object = Object as Core.Data.CCEPColumn;

                    m_AnatomicColumnModifier.IsActive = false;
                    m_IEEGColumnModifier.IsActive = false;
                    m_CCEPColumnModifier.IsActive = true;
                    m_FMRIColumnModifier.IsActive = false;
                    m_MEGColumnModifier.IsActive = false;
                }
                else if (type == typeof(Core.Data.FMRIColumn))
                {
                    if (!(m_Object is Core.Data.FMRIColumn)) Object = new Core.Data.FMRIColumn(Object.Name, Object.BaseConfiguration);
                    m_FMRIColumnModifier.Object = Object as Core.Data.FMRIColumn;

                    m_AnatomicColumnModifier.IsActive = false;
                    m_IEEGColumnModifier.IsActive = false;
                    m_CCEPColumnModifier.IsActive = false;
                    m_FMRIColumnModifier.IsActive = true;
                    m_MEGColumnModifier.IsActive = false;
                }
                else if (type == typeof(Core.Data.MEGColumn))
                {
                    if (!(m_Object is Core.Data.MEGColumn)) Object = new Core.Data.MEGColumn(Object.Name, Object.BaseConfiguration);
                    m_MEGColumnModifier.Object = Object as Core.Data.MEGColumn;

                    m_AnatomicColumnModifier.IsActive = false;
                    m_IEEGColumnModifier.IsActive = false;
                    m_CCEPColumnModifier.IsActive = false;
                    m_FMRIColumnModifier.IsActive = false;
                    m_MEGColumnModifier.IsActive = true;
                }
            }
        }
        void OnChangeNameHanlder(string value)
        {
            if (m_Object != null)
            {
                Object.Name = value;
            }
            OnChangeName.Invoke(value);
        }
        #endregion
    }
}