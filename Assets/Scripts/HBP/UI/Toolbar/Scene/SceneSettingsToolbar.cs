using UnityEngine;

namespace HBP.UI.Toolbar
{
    public class SceneSettingsToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Hide / show the left / right parts of the brain mesh
        /// </summary>
        [SerializeField] private BrainMeshes m_BrainMeshes;
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        [SerializeField] private BrainSelector m_BrainSelector;
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        [SerializeField] private MRISelector m_MRISelector;
        /// <summary>
        /// Change brain type (grey, white, inflated)
        /// </summary>
        [SerializeField] private ImplantationSelector m_ImplantationSelector;
        /// <summary>
        /// Threshold MRI parameters
        /// </summary>
        [SerializeField] private MRIContrast m_MRIContrast;
        /// <summary>
        /// Change IEEG colormap
        /// </summary>
        [SerializeField] private Colormap m_Colormap;
        /// <summary>
        /// Change brain surface color
        /// </summary>
        [SerializeField] private BrainColor m_BrainColor;
        /// <summary>
        /// Change brain cut color
        /// </summary>
        [SerializeField] private CutColor m_CutColor;
        /// <summary>
        /// Show / hide edges
        /// </summary>
        [SerializeField] private EdgeMode m_EdgeMode;
        /// <summary>
        /// Show / hide edges
        /// </summary>
        [SerializeField] private CutMode m_CutMode;
        /// <summary>
        /// Change the transparency of the brain
        /// </summary>
        [SerializeField] private TransparentBrain m_TransparentBrain;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_BrainMeshes);
            m_Tools.Add(m_BrainSelector);
            m_Tools.Add(m_MRISelector);
            m_Tools.Add(m_ImplantationSelector);
            m_Tools.Add(m_Colormap);
            m_Tools.Add(m_BrainColor);
            m_Tools.Add(m_CutColor);
            m_Tools.Add(m_EdgeMode);
            m_Tools.Add(m_MRIContrast);
            m_Tools.Add(m_CutMode);
            m_Tools.Add(m_TransparentBrain);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_BrainSelector.OnChangeValue.AddListener((type) =>
            {
                m_BrainMeshes.ChangeBrainTypeCallback();
            });
        }
        #endregion
    }
}