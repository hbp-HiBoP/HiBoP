using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Preferences;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class AnatomyPreferencesSubModifier : SubModifier<AnatomicPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_SiteNameCorrectionToggle;
        [SerializeField] Toggle m_PreloadMeshesToggle;
        [SerializeField] Toggle m_PreloadMRIsToggle;
        [SerializeField] Toggle m_PreloadImplantationsToggle;
        [SerializeField] Toggle m_PreloadSinglePatientDataInMultiPatientVisualizationToggle;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_SiteNameCorrectionToggle.interactable = value;
                m_PreloadMeshesToggle.interactable = value;
                m_PreloadMRIsToggle.interactable = value;
                m_PreloadImplantationsToggle.interactable = value;
                m_PreloadSinglePatientDataInMultiPatientVisualizationToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_SiteNameCorrectionToggle.onValueChanged.AddListener(value => Object.SiteNameCorrection = value);
            m_PreloadMeshesToggle.onValueChanged.AddListener(value => Object.MeshPreloading = value);
            m_PreloadMRIsToggle.onValueChanged.AddListener(value => Object.MRIPreloading = value);
            m_PreloadImplantationsToggle.onValueChanged.AddListener(value => Object.ImplantationPreloading = value);
            m_PreloadSinglePatientDataInMultiPatientVisualizationToggle.onValueChanged.AddListener(value => Object.PreloadSinglePatientDataInMultiPatientVisualization = value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(AnatomicPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_SiteNameCorrectionToggle.isOn = objectToDisplay.SiteNameCorrection;
            m_PreloadMeshesToggle.isOn = objectToDisplay.MeshPreloading;
            m_PreloadMRIsToggle.isOn = objectToDisplay.MRIPreloading;
            m_PreloadImplantationsToggle.isOn = objectToDisplay.ImplantationPreloading;
            m_PreloadSinglePatientDataInMultiPatientVisualizationToggle.isOn = objectToDisplay.PreloadSinglePatientDataInMultiPatientVisualization;
        }
        #endregion
    }
}

