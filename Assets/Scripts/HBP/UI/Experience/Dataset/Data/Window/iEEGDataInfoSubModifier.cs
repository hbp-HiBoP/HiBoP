using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class iEEGDataInfoSubModifier : SubModifier<d.iEEGDataInfo>
    {
        #region Properties     
        [SerializeField] Dropdown m_NormalizationDropdown;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_NormalizationDropdown.interactable = value;
            }
        }

        public override d.iEEGDataInfo Object
        {
            get => base.Object;
            set
            {
                base.Object = value;
                m_NormalizationDropdown.value = (int)value.Normalization;
                m_NormalizationDropdown.RefreshShownValue();
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_NormalizationDropdown.options = (from name in System.Enum.GetNames(typeof(d.iEEGDataInfo.NormalizationType)) select new Dropdown.OptionData(name, null)).ToList();
            m_NormalizationDropdown.onValueChanged.AddListener((value) => m_Object.Normalization = (d.iEEGDataInfo.NormalizationType)value);
        }
        #endregion
    }
}