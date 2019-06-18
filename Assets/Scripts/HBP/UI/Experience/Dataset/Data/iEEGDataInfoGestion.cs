using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class iEEGDataInfoGestion : MonoBehaviour
    {
        #region Properties     
        [SerializeField] Dropdown m_NormalizationDropdown;

        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_NormalizationDropdown.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(d.iEEGDataInfo iEEGDataInfo)
        {
            // Normalization.
            m_NormalizationDropdown.options = (from name in System.Enum.GetNames(typeof(d.iEEGDataInfo.NormalizationType)) select new Dropdown.OptionData(name, null)).ToList();
            m_NormalizationDropdown.value = (int)iEEGDataInfo.Normalization;

            m_NormalizationDropdown.RefreshShownValue();
            m_NormalizationDropdown.onValueChanged.RemoveAllListeners();
            m_NormalizationDropdown.onValueChanged.AddListener((value) => iEEGDataInfo.Normalization = (d.iEEGDataInfo.NormalizationType)value);
        }
        #endregion
    }
}