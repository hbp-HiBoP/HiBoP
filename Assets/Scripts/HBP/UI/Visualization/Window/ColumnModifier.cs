using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Visualization;
using Tools.Unity;
using UnityEngine.Events;

namespace HBP.UI.Visualization
{
    public class ColumnModifier : MonoBehaviour
    {
        #region Properties
        bool m_Interactable;
        public bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_NameInputField.interactable = value;
                m_TypeDropdown.interactable = value;
                m_IEEGColumnModifier.Interactable = value;
            }
        }

        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] IEEGColumnModifier m_IEEGColumnModifier;

        List<Data.Patient> m_Patients;
        BaseColumn m_Column;
        BaseColumn Column
        {
            get
            {
                return m_Column;
            }
            set
            {
                m_Column = value;
                OnChangeColumn.Invoke(value);
            }
        }

        public GenericEvent<string> OnChangeName { get; } = new GenericEvent<string>();
        public GenericEvent<BaseColumn> OnChangeColumn { get; } = new GenericEvent<BaseColumn>();
        #endregion

        #region Public Methods
        public void Set(BaseColumn column,IEnumerable<Data.Patient> patients)
        {
            m_Column = column;
            m_Patients = patients.ToList();

            if (m_Column == null)
            {
                m_NameInputField.interactable = false;
                m_NameInputField.text = "";
            }
            else
            {
                m_NameInputField.interactable = m_Interactable;
                m_NameInputField.text = m_Column.Name;
            }
            SetTypeDropdown();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_NameInputField.onValueChanged.AddListener((name) =>
            {
                if (m_Column != null)
                {
                    m_Column.Name = name;
                }
                OnChangeName.Invoke(name);
            });
            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
        }

        // Type.
        void SetTypeDropdown()
        {
            if (m_Column == null)
            {
                m_TypeDropdown.interactable = false;
                m_TypeDropdown.Set(typeof(Data.Enums.ColumnType), 0);
                m_IEEGColumnModifier.Set(null, m_Patients);
            }
            else
            {
                m_TypeDropdown.interactable = m_Interactable;
                Data.Enums.ColumnType type;
                if (m_Column is IEEGColumn) type = Data.Enums.ColumnType.iEEG;
                else type = Data.Enums.ColumnType.Anatomic;
                m_TypeDropdown.Set(typeof(Data.Enums.ColumnType), (int)type);
                OnChangeType((int)type);
            }
        }
        void OnChangeType(int value)
        {
            if (m_Column != null)
            {
                switch ((Data.Enums.ColumnType)value)
                {
                    case Data.Enums.ColumnType.Anatomic:
                        if (!(m_Column is AnatomicColumn)) Column = new AnatomicColumn(m_Column.Name, m_Column.BaseConfiguration);
                        m_IEEGColumnModifier.gameObject.SetActive(false);
                        break;
                    case Data.Enums.ColumnType.iEEG:
                        if (!(m_Column is IEEGColumn)) Column = new IEEGColumn(m_Column.Name, m_Column.BaseConfiguration, m_Patients);
                        m_IEEGColumnModifier.gameObject.SetActive(true);
                        m_IEEGColumnModifier.Set(m_Column as IEEGColumn, m_Patients);
                        break;
                }
            }
        }   
        #endregion
    }
}