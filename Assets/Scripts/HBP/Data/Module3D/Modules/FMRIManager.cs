using HBP.Core.Object3D;
using System.Linq;
using UnityEngine;

namespace HBP.Data.Module3D
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
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private string m_SelectedDiFuMoAtlas;
        public string SelectedDiFuMoAtlas
        {
            get
            {
                if (string.IsNullOrEmpty(m_SelectedDiFuMoAtlas))
                {
                    m_SelectedDiFuMoAtlas = Object3DManager.DiFuMo.FMRIs.Keys.FirstOrDefault();
                }
                return m_SelectedDiFuMoAtlas;
            }
            set
            {
                m_SelectedDiFuMoAtlas = value;
                m_SelectedDiFuMoArea = 0;
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private bool m_DisplayLocalizers;
        /// <summary>
        /// Do we display the Localizers on the cuts ?
        /// </summary>
        public bool DisplayLocalizers
        {
            get
            {
                return m_DisplayLocalizers;
            }
            set
            {
                m_DisplayLocalizers = value;
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private string m_SelectedLocalizersProtocol;
        /// <summary>
        /// Currently selected Localizers protocol
        /// </summary>
        public string SelectedLocalizersProtocol
        {
            get
            {
                if (string.IsNullOrEmpty(m_SelectedLocalizersProtocol))
                {
                    m_SelectedLocalizersProtocol = Object3DManager.Localizers.Protocols.FirstOrDefault()?.Name;
                }
                return m_SelectedLocalizersProtocol;
            }
            set
            {
                m_SelectedLocalizersProtocol = value;
                m_SelectedLocalizersBloc = GetFirstBlocNameForProtocol(value);
                m_SelectedLocalizersTimelineIndex = 0;
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private string m_SelectedLocalizersBloc;
        /// <summary>
        /// Currently selected Localizers bloc
        /// </summary>
        public string SelectedLocalizersBloc
        {
            get
            {
                if (string.IsNullOrEmpty(m_SelectedLocalizersBloc))
                {
                    m_SelectedLocalizersBloc = GetFirstBlocNameForProtocol(SelectedLocalizersProtocol);
                }
                return m_SelectedLocalizersBloc;
            }
            set
            {
                m_SelectedLocalizersBloc = value;
                m_SelectedLocalizersTimelineIndex = 0;
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private int m_SelectedLocalizersTimelineIndex;
        /// <summary>
        /// Currently selected timeline index for the Localizers bloc
        /// </summary>
        public int SelectedLocalizersTimelineIndex
        {
            get
            {
                // Ensure the index is within the valid range for the current FMRI
                var currentFMRI = GetCurrentLocalizersFMRI();
                if (currentFMRI != null && currentFMRI.Volumes.Count > 0)
                {
                    return Mathf.Clamp(m_SelectedLocalizersTimelineIndex, 0, currentFMRI.Volumes.Count - 1);
                }
                return 0;
            }
            set
            {
                var currentFMRI = GetCurrentLocalizersFMRI();
                if (currentFMRI != null && currentFMRI.Volumes.Count > 0)
                {
                    m_SelectedLocalizersTimelineIndex = Mathf.Clamp(value, 0, currentFMRI.Volumes.Count - 1);
                }
                else
                {
                    m_SelectedLocalizersTimelineIndex = 0;
                }
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        /// <summary>
        /// Currently used volume (depends on the type of fMRI we are displaying)
        /// </summary>
        public Core.DLL.Volume CurrentVolume
        {
            get
            {
                if (m_DisplayIBCContrasts)
                {
                    return Object3DManager.IBC.FMRI.Volumes[m_SelectedIBCContrastID];
                }
                else if (m_DisplayDiFuMo)
                {
                    return Object3DManager.DiFuMo.FMRIs[m_SelectedDiFuMoAtlas].Volumes[m_SelectedDiFuMoArea];
                }
                else if (m_DisplayLocalizers)
                {
                    var currentFMRI = GetCurrentLocalizersFMRI();
                    if (currentFMRI != null && currentFMRI.Volumes.Count > SelectedLocalizersTimelineIndex)
                    {
                        return currentFMRI.Volumes[SelectedLocalizersTimelineIndex];
                    }
                }
                return null;
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
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private const float m_DiFuMoNegativeMin = 0;
        private const float m_DiFuMoNegativeMax = 1;
        private const float m_DiFuMoPositiveMin = 0;
        private const float m_DiFuMoPositiveMax = 1;
        private const float m_DiFuMoAlpha = 1f;

        private const float m_LocalizersNegativeMin = 0;
        private const float m_LocalizersNegativeMax = 1;
        private const float m_LocalizersPositiveMin = 0;
        private const float m_LocalizersPositiveMax = 1;
        private const float m_LocalizersAlpha = 1f;
        #endregion

        #region Private Methods
        /// <summary>
        /// Get the first bloc name for a given protocol
        /// </summary>
        /// <param name="protocolName">Name of the protocol</param>
        /// <returns>First bloc name or null</returns>
        private string GetFirstBlocNameForProtocol(string protocolName)
        {
            if (string.IsNullOrEmpty(protocolName))
                return null;

            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolName);
            return protocol?.Blocs.FirstOrDefault()?.Name;
        }

        /// <summary>
        /// Get the current FMRI for the selected protocol and bloc
        /// </summary>
        /// <returns>Current FMRI or null</returns>
        private FMRI GetCurrentLocalizersFMRI()
        {
            if (string.IsNullOrEmpty(SelectedLocalizersProtocol) || string.IsNullOrEmpty(SelectedLocalizersBloc))
                return null;

            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == SelectedLocalizersProtocol);
            var bloc = protocol?.Blocs.FirstOrDefault(b => b.Name == SelectedLocalizersBloc);
            return bloc?.FMRI;
        }
        #endregion

        #region Public Methods
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
                    colors = CurrentVolume.ConvertValuesToColors(m_FMRIValues, m_DiFuMoNegativeMin, m_DiFuMoNegativeMax, m_DiFuMoPositiveMin, m_DiFuMoPositiveMax, m_DiFuMoAlpha);
                }
                else if (m_DisplayLocalizers)
                {
                    colors = CurrentVolume.ConvertValuesToColors(m_FMRIValues, m_LocalizersNegativeMin, m_LocalizersNegativeMax, m_LocalizersPositiveMin, m_LocalizersPositiveMax, m_LocalizersAlpha);
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

        internal void ColorCuts(Column3D column)
        {
            if (m_DisplayIBCContrasts) 
            {
                column.CutTextures.ColorCutsTexturesWithFMRIAtlas(CurrentVolume, m_FMRINegativeCalMinFactor, m_FMRINegativeCalMaxFactor, m_FMRIPositiveCalMinFactor, m_FMRIPositiveCalMaxFactor, m_FMRIAlpha);
            }
            else if (m_DisplayDiFuMo) 
            {
                column.CutTextures.ColorCutsTexturesWithFMRIAtlas(CurrentVolume, m_DiFuMoNegativeMin, m_DiFuMoNegativeMax, m_DiFuMoPositiveMin, m_DiFuMoPositiveMax, m_DiFuMoAlpha);
            }
            else if (m_DisplayLocalizers) 
            {
                column.CutTextures.ColorCutsTexturesWithFMRIAtlas(CurrentVolume, m_LocalizersNegativeMin, m_LocalizersNegativeMax, m_LocalizersPositiveMin, m_LocalizersPositiveMax, m_LocalizersAlpha);
            }
        }
        #endregion
    }
}