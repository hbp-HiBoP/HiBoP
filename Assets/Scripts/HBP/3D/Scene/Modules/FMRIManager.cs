using System.Collections.Generic;
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
                UpdateSurfaceFMRIValues();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
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
                ApplicationState.Module3D.IBCObjects.UpdateCurrentVolume(m_SelectedIBCContrastID);
                UpdateSurfaceFMRIValues();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
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
                ApplicationState.Module3D.IBCObjects.UpdateCurrentVolume(m_SelectedIBCContrastID);
                UpdateSurfaceFMRIValues();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private bool m_DisplayDiFuMo;
        public bool DisplayDiFuMo
        {
            get
            {
                return m_DisplayDiFuMo;
            }
            set
            {
                m_DisplayDiFuMo = value;
                ApplicationState.Module3D.DiFuMoObjects.UpdateCurrentVolume(m_SelectedDiFuMoArea);
                UpdateSurfaceFMRIValues();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private int m_SelectedDiFuMoArea;
        public int SelectedDiFuMoArea
        {
            get
            {
                return m_SelectedDiFuMoArea;
            }
            set
            {
                m_SelectedDiFuMoArea = value;
                ApplicationState.Module3D.DiFuMoObjects.UpdateCurrentVolume(m_SelectedDiFuMoArea);
                UpdateSurfaceFMRIValues();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
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
                    return ApplicationState.Module3D.IBCObjects.Volume;
                }
                else if (m_DisplayDiFuMo)
                {
                    return ApplicationState.Module3D.DiFuMoObjects.Volume;
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

        private float[] m_FMRIValues;

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
                UpdateSurfaceFMRIColors();
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
                UpdateSurfaceFMRIColors();
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
                UpdateSurfaceFMRIColors();
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
                UpdateSurfaceFMRIColors();
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
                UpdateSurfaceFMRIColors();
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
        public void UpdateSurfaceFMRIValues()
        {
            m_Scene.BrainMaterials.SetDisplayFMRI(DisplayFMRI);
            if (CurrentVolume != null)
                m_FMRIValues = CurrentVolume.GetVerticesValues(m_Scene.MeshManager.BrainSurface);

            UpdateSurfaceFMRIColors();
        }
        /// <summary>
        /// Update all colors for the FMRI for all vertices
        /// </summary>
        public void UpdateSurfaceFMRIColors()
        {
            if (CurrentVolume != null)
            {
                Color[] colors;
                if (m_DisplayDiFuMo)
                {
                    colors = CurrentVolume.ConvertValuesToColors(m_FMRIValues, 0, 1, 0, 1, 1);
                }
                else
                {
                    colors = CurrentVolume.ConvertValuesToColors(m_FMRIValues, m_FMRINegativeCalMinFactor, m_FMRINegativeCalMaxFactor, m_FMRIPositiveCalMinFactor, m_FMRIPositiveCalMaxFactor, m_FMRIAlpha);
                }
                m_DisplayedObjects.Brain.GetComponent<MeshFilter>().mesh.colors = colors;
                foreach (Column3D column in m_Scene.Columns)
                {
                    column.BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors = colors;
                }
            }
            m_Scene.SceneInformation.BaseCutTexturesNeedUpdate = true;
        }
        #endregion
    }
}