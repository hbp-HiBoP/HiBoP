using HBP.Core.Tools;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    /// <summary>Distance and colormap tools for anatomical density, using the fixed reference distribution.</summary>
    public class DensityParameters : Tool
    {
        [SerializeField] private InputField m_InputField;
        [SerializeField] private Button m_Button;
        [SerializeField] private Button m_Auto;
        [SerializeField] private ThresholdIEEG m_Threshold;
        [SerializeField] private Tools.Tooltip m_Tooltip;
        public bool IsGlobal { get; set; }

        public override void Initialize()
        {
            m_InputField.onEndEdit.AddListener(value =>
            {
                if (ListenerLock || SelectedColumn is not Column3DAnatomy) return;
                if (NumberExtension.TryParseFloat(value, out float distance) && ThresholdRatioUtility.IsFinite(distance))
                    foreach (Column3DAnatomy column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                        column.AnatomyParameters.InfluenceDistance = distance;
                UpdateStatus();
            });
            m_Threshold.Initialize();
            m_Threshold.OnChangeValues.AddListener((min, middle, max) =>
            {
                if (ListenerLock || SelectedColumn is not Column3DAnatomy) return;
                // Global thresholds share ratios; each column resolves them against its own projected maximum.
                foreach (Column3DAnatomy column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                    column.AnatomyParameters.SetSpanRatios(min / 100f, middle / 100f, max / 100f);
            });
            m_Auto.onClick.AddListener(() =>
            {
                if (ListenerLock || SelectedColumn is not Column3DAnatomy) return;
                foreach (Column3DAnatomy column in GetColumnsDependingOnTypeAndGlobal(IsGlobal)) column.AnatomyParameters.ResetSpanValues();
                UpdateStatus();
            });
        }

        public override void DefaultState()
        {
            gameObject.SetActive(false);
            m_InputField.interactable = false;
            m_Button.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool anatomy = SelectedColumn is Column3DAnatomy;
            gameObject.SetActive(anatomy);
            m_InputField.interactable = anatomy;
            m_Button.interactable = anatomy;
            m_Auto.interactable = anatomy;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn is not Column3DAnatomy column) return;
            m_InputField.SetTextWithoutNotify(column.AnatomyParameters.InfluenceDistance.ToString("N2"));
            m_Threshold.UpdateDensityValues(column);
            m_Tooltip.Text = "Density colormap thresholds (%)\nHistogram: relative density at 15 mm, all contacts (fixed reference).\nValues: percentages of the current projection's maximum density.\nAuto: 5%, 35%, 65%.";
            if (column.DensityHistogramBins == null) m_Tooltip.Text += "\nDensity reference not computed yet.";
        }
    }
}
