using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ThresholdIEEG : Tool
    {
        #region Properties
        [SerializeField] private Button m_Button;
        [SerializeField] private Button m_Auto;
        [SerializeField] private Module3D.ThresholdIEEG m_ThresholdIEEG;
        
        public bool IsGlobal { get; set; }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_ThresholdIEEG.Initialize();
            m_ThresholdIEEG.OnChangeValues.AddListener((min, mid, max) =>
            {
                if (ListenerLock) return;

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.DynamicParameters.SetSpanValues(min, mid, max);
                }
            });
            m_Auto.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.DynamicParameters.ResetSpanValues(column);
                    m_ThresholdIEEG.UpdateIEEGValues(column);
                }
            });
        }

        public override void DefaultState()
        {
            m_Button.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = SelectedColumn is Column3DIEEG;
            bool isColumnCCEPAndSourceSelected = SelectedColumn is HBP.Module3D.Column3DCCEP ccepColumn && ccepColumn.IsSourceSelected;

            m_Button.interactable = isColumnIEEG || isColumnCCEPAndSourceSelected;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn is Column3DDynamic dynamicColumn)
            {
                m_ThresholdIEEG.UpdateIEEGValues(dynamicColumn);
            }
        }
        #endregion
    }
}