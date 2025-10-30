using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class ExportActivityColumnItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_ColumnNameText;
        [SerializeField] Toggle m_Toggle;
        [SerializeField] InputField m_FileNameInputField;
        [SerializeField] Text m_ExtensionText;
        [SerializeField] GameObject m_ParametersContainer;
        [SerializeField] GameObject m_ErrorMessageContainer;

        public bool IsSelected { get { return m_Toggle.isOn; } }
        public string FileName { get { return m_FileNameInputField.text + m_ExtensionText.text; } }
        public string FileNameWithoutExtension
        {
            get { return m_FileNameInputField.text; }
            set { m_FileNameInputField.text = value; }
        }
        public string Extension
        {
            get { return m_ExtensionText.text; }
            set { m_ExtensionText.text = value; }
        }
        public Column3D AssociatedColumn { get; private set; }
        #endregion

        #region Public Methods
        public void Initialize(Column3D column)
        {
            AssociatedColumn = column;
            m_ColumnNameText.text = column.Name;
            m_FileNameInputField.text = column.Name;
            m_Toggle.isOn = false;

            bool canExportColumn = column is Column3DIEEG;
            m_Toggle.interactable = canExportColumn;
            m_ParametersContainer.SetActive(canExportColumn);
            m_ErrorMessageContainer.SetActive(!canExportColumn);
        }
        #endregion
    }
}