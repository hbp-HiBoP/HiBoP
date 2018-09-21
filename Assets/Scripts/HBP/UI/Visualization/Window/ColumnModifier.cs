using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Visualization;
using Tools.Unity;

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
                m_NameInputField.interactable = value;
                m_TypeDropdown.interactable = value;
            }
        }

        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] IEEGColumnModifier m_IEEGColumnModifier;

        List<Data.Patient> m_Patients;
        BaseColumn Column;
        #endregion

        #region Public Methods
        public void SetTab(BaseColumn column,IEnumerable<Data.Patient> patients)
        {
            Column = column;

            m_Patients = patients.ToList();

            m_NameInputField.text = Column.Name;
            SetTypeDropdown();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => Column.Name = name);

            m_TypeDropdown.onValueChanged.RemoveAllListeners();
            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);

        }

        // Type.
        void SetTypeDropdown()
        {
            Data.Enums.ColumnType type;
            if (Column is IEEGColumn) type = Data.Enums.ColumnType.iEEG;
            else type = Data.Enums.ColumnType.Anatomic;
            m_TypeDropdown.Set(typeof(Data.Enums.ColumnType), (int) type);
        }
        void OnChangeType(int value)
        {
            switch((Data.Enums.ColumnType)value)
            {
                case Data.Enums.ColumnType.Anatomic:
                    Column = new AnatomicColumn(Column.Name,Column.BaseConfiguration);
                    m_IEEGColumnModifier.Interactable = false;
                    m_IEEGColumnModifier.gameObject.SetActive(false);
                    break;
                case Data.Enums.ColumnType.iEEG:
                    Column = new IEEGColumn(Column.Name, Column.BaseConfiguration, m_Patients);
                    m_IEEGColumnModifier.gameObject.SetActive(true);
                    m_IEEGColumnModifier.Set(Column as IEEGColumn, m_Patients);
                    break;
            }
        }   
        #endregion
    }
}