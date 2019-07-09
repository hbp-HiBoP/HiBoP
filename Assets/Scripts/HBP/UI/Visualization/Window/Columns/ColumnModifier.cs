using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Visualization;
using Tools.Unity;
using UnityEngine.Events;
using System;

namespace HBP.UI.Visualization
{
    public class ColumnModifier : SubModifier<Column>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] IEEGColumnModifier m_IEEGColumnModifier;
        [SerializeField] CCEPColumnModifier m_CCEPColumnModifier;
        [SerializeField] AnatomicColumnModifier m_AnatomicColumnModifier;

        Type[] m_Types;

        public override Column Object
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
                    m_TypeDropdown.value = Array.IndexOf(m_Types, value.GetType());
                }
                else
                {
                    m_NameInputField.interactable = false;
                    m_NameInputField.text = "";
                    m_TypeDropdown.interactable = false;
                    m_TypeDropdown.value = Array.IndexOf(m_Types, typeof(AnatomicColumn));
                }
                OnChangeColumn.Invoke(value);
            }
        }

        Data.Patient[] m_Patients;
        public Data.Patient[] Patients
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
                m_AnatomicColumnModifier.Interactable = value;
            }
        }

        public GenericEvent<string> OnChangeName { get; } = new GenericEvent<string>();
        public GenericEvent<Column> OnChangeColumn { get; } = new GenericEvent<Column>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener(OnChangeNameHanlder);
            m_TypeDropdown.onValueChanged.AddListener(OnChangeTypeHandler);
            m_Types = m_TypeDropdown.Set(typeof(Column));
        }
        #endregion

        #region Private Methods
        void OnChangeTypeHandler(int value)
        {
            Type type = m_Types[value];
            if (m_Object != null)
            {
                if (type == typeof(AnatomicColumn))
                {
                    if (!(m_Object is AnatomicColumn)) Object = new AnatomicColumn(m_Object.Name, m_Object.BaseConfiguration);
                    m_AnatomicColumnModifier.Object = Object as AnatomicColumn;

                    m_AnatomicColumnModifier.IsActive = true;
                    m_IEEGColumnModifier.IsActive = false;
                    m_CCEPColumnModifier.IsActive = false;
                }
                else if (type == typeof(IEEGColumn))
                {
                    if (!(m_Object is IEEGColumn)) Object = new IEEGColumn(m_Object.Name, m_Object.BaseConfiguration, m_Patients);
                    m_IEEGColumnModifier.Object = Object as IEEGColumn;

                    m_AnatomicColumnModifier.IsActive = false;
                    m_IEEGColumnModifier.IsActive = true;
                    m_CCEPColumnModifier.IsActive = false;
                }
                else if (type == typeof(CCEPColumn))
                {
                    if (!(m_Object is CCEPColumn)) Object = new CCEPColumn(m_Object.Name, m_Object.BaseConfiguration, m_Patients);
                    m_CCEPColumnModifier.Object = Object as CCEPColumn;

                    m_AnatomicColumnModifier.IsActive = false;
                    m_IEEGColumnModifier.IsActive = false;
                    m_CCEPColumnModifier.IsActive = true;
                }
            }
        }
        void OnChangeNameHanlder(string value)
        {
            if (m_Object != null)
            {
                m_Object.Name = value;
            }
            OnChangeName.Invoke(value);
        }
        #endregion
    }
}