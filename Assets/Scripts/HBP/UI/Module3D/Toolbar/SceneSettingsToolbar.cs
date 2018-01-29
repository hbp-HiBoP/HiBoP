using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class SceneSettingsToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Hide / show the left / right parts of the brain mesh
        /// </summary>
        [SerializeField]
        private Tools.BrainMeshes m_BrainMeshes;
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        [SerializeField]
        private Tools.BrainSelector m_BrainSelector;
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        [SerializeField]
        private Tools.MRISelector m_MRISelector;
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        [SerializeField]
        private Tools.ImplantationSelector m_ImplantationSelector;
        /// <summary>
        /// Show / hide Mars Atlas
        /// </summary>
        [SerializeField]
        private Tools.MarsAtlas m_MarsAtlas;
        /// <summary>
        /// Threshold MRI parameters
        /// </summary>
        [SerializeField]
        private Tools.ThresholdMRI m_ThresholdMRI;
        /// <summary>
        /// Change IEEG colormap
        /// </summary>
        [SerializeField]
        private Tools.Colormap m_Colormap;
        /// <summary>
        /// Change brain surface color
        /// </summary>
        [SerializeField]
        private Tools.BrainColor m_BrainColor;
        /// <summary>
        /// Change brain cut color
        /// </summary>
        [SerializeField]
        private Tools.CutColor m_CutColor;
        /// <summary>
        /// Show / hide edges
        /// </summary>
        [SerializeField]
        private Tools.EdgeMode m_EdgeMode;
        /// <summary>
        /// Show / hide edges
        /// </summary>
        [SerializeField]
        private Tools.CutMode m_CutMode;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void AddTools()
        {
            m_Tools.Add(m_BrainMeshes);
            m_Tools.Add(m_BrainSelector);
            m_Tools.Add(m_MRISelector);
            m_Tools.Add(m_ImplantationSelector);
            m_Tools.Add(m_MarsAtlas);
            m_Tools.Add(m_Colormap);
            m_Tools.Add(m_BrainColor);
            m_Tools.Add(m_CutColor);
            m_Tools.Add(m_EdgeMode);
            m_Tools.Add(m_ThresholdMRI);
            m_Tools.Add(m_CutMode);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_BrainSelector.OnChangeValue.AddListener((type) =>
            {
                m_MarsAtlas.ChangeBrainTypeCallback();
                m_BrainMeshes.ChangeBrainTypeCallback();
                UpdateInteractableButtons();
            });
            m_MRISelector.OnChangeValue.AddListener((type) =>
            {
                UpdateButtonsStatus(UpdateToolbarType.Scene);
            });
        }
        #endregion
    }
}