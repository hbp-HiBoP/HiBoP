using HBP.Core.Data.Enums;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Experience.Dataset
{
    public class iEEGDataInfoSubModifier : SubModifier<Core.Data.IEEGDataInfo>
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
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_NormalizationDropdown.options = (from name in System.Enum.GetNames(typeof(NormalizationType)) select new Dropdown.OptionData(name, null)).ToList();
            m_NormalizationDropdown.onValueChanged.AddListener((value) => Object.Normalization = (NormalizationType)value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.IEEGDataInfo objectToDisplay)
        {
            m_NormalizationDropdown.value = (int)objectToDisplay.Normalization;
            m_NormalizationDropdown.RefreshShownValue();
        }
        #endregion
    }
}