using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    public class FMRIManager
    {
        #region Properties
        private enum CalType { Value, Factor }
        /// <summary>
        /// Cal type (depending on how we set the cal values)
        /// </summary>
        private CalType m_CurrentCalType = CalType.Factor;

        private MRI3D m_FMRI;
        /// <summary>
        /// FMRI associated to this scene
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
                OnChangeFMRIParameters.Invoke();
            }
        }

        private bool m_DisplayIBCContrasts;
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
                OnChangeFMRIParameters.Invoke();
            }
        }
        private int m_SelectedIBCContrastID;
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
                OnChangeFMRIParameters.Invoke();
            }
        }
        public IBC.Contrast SelectedIBCContrast
        {
            get
            {
                return ApplicationState.Module3D.IBCObjects.Contrasts[m_SelectedIBCContrastID];
            }
        }

        /// <summary>
        /// Current used volume
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
                OnChangeFMRIParameters.Invoke();
            }
        }

        private float m_FMRICalMinFactor = 0.4f;
        /// <summary>
        /// Cal min factor of the FMRI
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
                OnChangeFMRIParameters.Invoke();
            }
        }

        private float m_FMRICalMin;
        /// <summary>
        /// Cal min value of the FMRI
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
                OnChangeFMRIParameters.Invoke();
            }
        }

        private float m_FMRICalMaxFactor = 0.6f;
        /// <summary>
        /// Cal max factor of the FMRI
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
                OnChangeFMRIParameters.Invoke();
            }
        }

        private float m_FMRICalMax;
        /// <summary>
        /// Cal max value of the FMRI
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
                OnChangeFMRIParameters.Invoke();
            }
        }

        public UnityEvent OnChangeFMRIParameters = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Color the cut with a FMRI
        /// </summary>
        /// <param name="column"></param>
        /// <param name="cutID"></param>
        /// <param name="blurFactor"></param>
        public void ColorCutTexture(Column3D column, int cutID, float blurFactor)
        {
            column.CutTextures.ColorCutsTexturesWithFMRI(CurrentVolume, cutID, m_FMRICalMinFactor, m_FMRICalMaxFactor, m_FMRIAlpha);
        }
        #endregion

        #region Private Methods
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
        }
        #endregion
    }
}