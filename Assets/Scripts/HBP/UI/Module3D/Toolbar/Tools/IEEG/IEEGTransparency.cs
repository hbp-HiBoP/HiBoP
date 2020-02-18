using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IEEGTransparency : Tool
    {
        #region Properties
        /// <summary>
        /// Slider to control the alpha of the iEEG on the brain
        /// </summary>
        [SerializeField] private Slider m_Slider;
        /// <summary>
        /// Are the changes applied to all columns ?
        /// </summary>
        public bool IsGlobal { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;
                
                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.DynamicParameters.AlphaMin = value;
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Slider.value = 0.2f;
            m_Slider.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamic = SelectedColumn is HBP.Module3D.Column3DDynamic;

            m_Slider.interactable = isColumnDynamic;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedColumn is HBP.Module3D.Column3DDynamic dynamicColumn)
            {
                m_Slider.value = dynamicColumn.DynamicParameters.AlphaMin;
            }
            else
            {
                m_Slider.value = 0.2f;
            }
        }
        #endregion
    }
}