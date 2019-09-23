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

        private float m_FMRIAlpha = 0.5f;
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

        private float m_FMRICalMinFactor = 0.4f;
        /// <summary>
        /// Calibration min factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRICalMinFactor
        {
            get
            {
                return m_FMRICalMinFactor;
            }
            set
            {
                m_FMRICalMinFactor = value;
                if (CurrentVolume != null)
                {
                    m_FMRICalMin = m_FMRICalMinFactor * (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin) + CurrentVolume.ExtremeValues.ComputedCalMin;
                }
                m_CurrentCalType = CalType.Factor;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRICalMin;
        /// <summary>
        /// Calibration min value of the FMRI
        /// </summary>
        public float FMRICalMin
        {
            get
            {
                return m_FMRICalMin;
            }
            set
            {
                m_FMRICalMin = value;
                if (CurrentVolume != null)
                {
                    m_FMRICalMinFactor = (value - CurrentVolume.ExtremeValues.ComputedCalMin) / (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin);
                }
                m_CurrentCalType = CalType.Value;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRICalMaxFactor = 0.6f;
        /// <summary>
        /// Calibration max factor of the FMRI (between 0 and 1)
        /// </summary>
        public float FMRICalMaxFactor
        {
            get
            {
                return m_FMRICalMaxFactor;
            }
            set
            {
                m_FMRICalMaxFactor = value;
                if (CurrentVolume != null)
                {
                    m_FMRICalMax = m_FMRICalMaxFactor * (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin) + CurrentVolume.ExtremeValues.ComputedCalMin;
                }
                m_CurrentCalType = CalType.Factor;
                m_Scene.ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_FMRICalMax;
        /// <summary>
        /// Calibration max value of the FMRI
        /// </summary>
        public float FMRICalMax
        {
            get
            {
                return m_FMRICalMax;
            }
            set
            {
                m_FMRICalMax = value;
                if (CurrentVolume != null)
                {
                    m_FMRICalMaxFactor = (value - CurrentVolume.ExtremeValues.ComputedCalMin) / (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin);
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
            column.CutTextures.ColorCutsTexturesWithFMRI(CurrentVolume, cutID, m_FMRICalMinFactor, m_FMRICalMaxFactor, m_FMRIAlpha);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Method called when changing the used fMRI of the scene
        /// </summary>
        private void ChangeFMRICallback()
        {
            if (CurrentVolume == null)
                return;

            switch (m_CurrentCalType)
            {
                case CalType.Value:
                    m_FMRICalMinFactor = (m_FMRICalMin - CurrentVolume.ExtremeValues.ComputedCalMin) / (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin);
                    m_FMRICalMaxFactor = (m_FMRICalMax - CurrentVolume.ExtremeValues.ComputedCalMin) / (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin);
                    break;
                case CalType.Factor:
                    m_FMRICalMin = m_FMRICalMinFactor * (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin) + CurrentVolume.ExtremeValues.ComputedCalMin;
                    m_FMRICalMax = m_FMRICalMaxFactor * (CurrentVolume.ExtremeValues.ComputedCalMax - CurrentVolume.ExtremeValues.ComputedCalMin) + CurrentVolume.ExtremeValues.ComputedCalMin;
                    break;
            }

            m_Scene.ResetIEEG();
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}