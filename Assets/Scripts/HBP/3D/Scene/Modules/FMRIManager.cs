using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Class responsible for the display of fMRIs on the cuts
    /// </summary>
    public class FMRIManager : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent scene of the manager
        /// </summary>
        [SerializeField] private Base3DScene m_Scene;
        /// <summary>
        /// Component containing references to GameObjects of the 3D scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        private enum CalType { Value, Factor }
        /// <summary>
        /// Calibration type (depending on how we set the calibration values)
        /// </summary>
        private CalType m_CurrentCalType = CalType.Factor;

        private MRI3D m_FMRI;
        /// <summary>
        /// FMRI associated to the scene
        /// </summary>
        public MRI3D FMRI
        {
            get
            {
                return m_FMRI;
            }
            set
            {
                m_FMRI = value;
                ChangeFMRICallback();
            }
        }

        private bool m_DisplayIBCContrasts;
        /// <summary>
        /// Do we display the IBC contrasts on the cuts ?
        /// </summary>
        public bool DisplayIBCContrasts
        {
            get
            {
                return m_DisplayIBCContrasts;
            }
            set
            {
                m_DisplayIBCContrasts = value;
                ChangeFMRICallback();
            }
        }

        private int m_SelectedIBCContrastID;
        /// <summary>
        /// ID of the selected IBC contrast
        /// </summary>
        public int SelectedIBCContrastID
        {
            get
            {
                return m_SelectedIBCContrastID;
            }
            set
            {
                m_SelectedIBCContrastID = value;
                ChangeFMRICallback();
            }
        }
        /// <summary>
        /// Selected IBC contrast (this object contains some information about the contrast)
        /// </summary>
        public IBC.Contrast SelectedIBCContrast
        {
            get
            {
                return ApplicationState.Module3D.IBCObjects.Contrasts[m_SelectedIBCContrastID];
            }
        }

        /// <summary>
        /// Currently used volume (depends on the type of fMRI we are displaying)
        /// </summary>
        public DLL.Volume CurrentVolume
        {
            get
            {
                if (FMRI != null)
                {
                    return FMRI.Volume;
                }
                else if (m_DisplayIBCContrasts)
                {
                    return SelectedIBCContrast.Volume;
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Do we display a FMRI ?
        /// </summary>
        public bool DisplayFMRI
        {
            get
            {
                return CurrentVolume != null;
            }
        }

        private float m_FMRIAlpha = 0.2f;
        /// <summary>
        /// Alpha of the FMRI
        /// </summary>
        public float FMRIAlpha
        {
            get
            {
                return m_FMRIAlpha;
            }
            set
            {
                m_FMRIAlpha = value;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRINegativeCalMinFactor = 0.05f;
        /// <summary>
        /// Calibration min factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRINegativeCalMinFactor
        {
            get
            {
                return m_FMRINegativeCalMinFactor;
            }
            set
            {
                m_FMRINegativeCalMinFactor = value;
                if (CurrentVolume != null)
                {
                    m_FMRINegativeCalMin = m_FMRINegativeCalMinFactor * CurrentVolume.ExtremeValues.Min;
                }
                m_CurrentCalType = CalType.Factor;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRINegativeCalMin;
        /// <summary>
        /// Calibration min value of the FMRI
        /// </summary>
        public float FMRINegativeCalMin
        {
            get
            {
                return m_FMRINegativeCalMin;
            }
            set
            {
                m_FMRINegativeCalMin = value;
                if (CurrentVolume != null)
                {
                    m_FMRINegativeCalMinFactor = value / CurrentVolume.ExtremeValues.Min;
                }
                m_CurrentCalType = CalType.Value;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRINegativeCalMaxFactor = 0.5f;
        /// <summary>
        /// Calibration min factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRINegativeCalMaxFactor
        {
            get
            {
                return m_FMRINegativeCalMaxFactor;
            }
            set
            {
                m_FMRINegativeCalMaxFactor = value;
                if (CurrentVolume != null)
                {
                    m_FMRINegativeCalMax = m_FMRINegativeCalMaxFactor * CurrentVolume.ExtremeValues.Min;
                }
                m_CurrentCalType = CalType.Factor;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRINegativeCalMax;
        /// <summary>
        /// Calibration min value of the FMRI
        /// </summary>
        public float FMRINegativeCalMax
        {
            get
            {
                return m_FMRINegativeCalMax;
            }
            set
            {
                m_FMRINegativeCalMax = value;
                if (CurrentVolume != null)
                {
                    m_FMRINegativeCalMaxFactor = value / CurrentVolume.ExtremeValues.Min;
                }
                m_CurrentCalType = CalType.Value;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRIPositiveCalMinFactor = 0.05f;
        /// <summary>
        /// Calibration max factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRIPositiveCalMinFactor
        {
            get
            {
                return m_FMRIPositiveCalMinFactor;
            }
            set
            {
                m_FMRIPositiveCalMinFactor = value;
                if (CurrentVolume != null)
                {
                    m_FMRIPositiveCalMin = m_FMRIPositiveCalMinFactor * CurrentVolume.ExtremeValues.Max;
                }
                m_CurrentCalType = CalType.Factor;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRIPositiveCalMin;
        /// <summary>
        /// Calibration max value of the FMRI
        /// </summary>
        public float FMRIPositiveCalMin
        {
            get
            {
                return m_FMRIPositiveCalMin;
            }
            set
            {
                m_FMRIPositiveCalMin = value;
                if (CurrentVolume != null)
                {
                    m_FMRIPositiveCalMinFactor = value / CurrentVolume.ExtremeValues.Max;
                }
                m_CurrentCalType = CalType.Value;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRIPositiveCalMaxFactor = 0.5f;
        /// <summary>
        /// Calibration max factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRIPositiveCalMaxFactor
        {
            get
            {
                return m_FMRIPositiveCalMaxFactor;
            }
            set
            {
                m_FMRIPositiveCalMaxFactor = value;
                if (CurrentVolume != null)
                {
                    m_FMRIPositiveCalMax = m_FMRIPositiveCalMaxFactor * CurrentVolume.ExtremeValues.Max;
                }
                m_CurrentCalType = CalType.Factor;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRIPositiveCalMax;
        /// <summary>
        /// Calibration max value of the FMRI
        /// </summary>
        public float FMRIPositiveCalMax
        {
            get
            {
                return m_FMRIPositiveCalMax;
            }
            set
            {
                m_FMRIPositiveCalMax = value;
                if (CurrentVolume != null)
                {
                    m_FMRIPositiveCalMaxFactor = value / CurrentVolume.ExtremeValues.Max;
                }
                m_CurrentCalType = CalType.Value;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Color the cut with the selected FMRI
        /// </summary>
        /// <param name="column">Column on which the cut is colored</param>
        /// <param name="cutID">ID of the cut to color</param>
        public void ColorCutTexture(Column3D column, int cutID)
        {
            column.CutTextures.ColorCutsTexturesWithFMRI(CurrentVolume, cutID, m_FMRINegativeCalMinFactor, m_FMRINegativeCalMaxFactor, m_FMRIPositiveCalMinFactor, m_FMRIPositiveCalMaxFactor, m_FMRIAlpha);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Method called when changing the used fMRI of the scene
        /// </summary>
        private void ChangeFMRICallback()
        {
            if (CurrentVolume != null)
            {
                switch (m_CurrentCalType)
                {
                    case CalType.Value:
                        m_FMRINegativeCalMinFactor = m_FMRINegativeCalMin / CurrentVolume.ExtremeValues.Min;
                        m_FMRIPositiveCalMinFactor = m_FMRIPositiveCalMin / CurrentVolume.ExtremeValues.Max;
                        break;
                    case CalType.Factor:
                        m_FMRINegativeCalMin = m_FMRINegativeCalMinFactor * CurrentVolume.ExtremeValues.Min;
                        m_FMRIPositiveCalMin = m_FMRIPositiveCalMinFactor * CurrentVolume.ExtremeValues.Max;
                        break;
                }
            }
            m_Scene.ResetIEEG();
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}