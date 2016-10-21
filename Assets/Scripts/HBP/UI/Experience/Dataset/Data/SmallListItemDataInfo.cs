using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class SmallListItemDataInfo : MonoBehaviour
    {
        #region Attributs
        [SerializeField]
        private Text m_experienceText;
        [SerializeField]
        private Text m_dataText;
        [SerializeField]
        private InputField m_positionInputField;

        public bool IsOn { get { return GetComponent<Toggle>().isOn; } set { GetComponent<Toggle>().isOn = value; } }
        public int Position { get { return GetPosition(); } }

        private DataInfo m_dataInfo;
        public DataInfo DataInfo { get { return m_dataInfo; } private set { SetDataInfo(value); } }
        #endregion

        #region Public Methods
        public void Set(string experience, DataInfo dataInfo)
        {
            DataInfo = dataInfo;
            m_experienceText.text = experience;
        }
        #endregion

        #region Private Methods
        void SetDataInfo(DataInfo dataInfo)
        {
            m_dataInfo = dataInfo;
            m_experienceText.text = dataInfo.Name;
        }

        int GetPosition()
        {
            return int.Parse(m_positionInputField.text);
        }
        #endregion
    }
}