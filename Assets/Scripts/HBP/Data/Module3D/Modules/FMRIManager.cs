using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using System.Linq;
using UnityEngine;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Class responsible for the display of fMRIs on the cuts
    /// </summary>
    public class FMRIManager : MonoBehaviour
    {
        public AtlasConfiguration CaptureConfiguration()
        {
            return new AtlasConfiguration(m_Scene.AtlasManager.DisplayMarsAtlas, m_Scene.AtlasManager.DisplayJuBrainAtlas, m_Scene.AtlasManager.AtlasAlpha, m_DisplayIBCContrasts, m_SelectedIBCContrastID, m_DisplayDiFuMo, m_SelectedDiFuMoAtlas, m_SelectedDiFuMoArea, m_DisplayLocalizers, m_SelectedLocalizersProtocol, m_SelectedLocalizersData, m_SelectedLocalizersBloc, m_SelectedLocalizersTimelineIndex, m_FMRIAlpha, m_FMRINegativeCalMinFactor, m_FMRINegativeCalMaxFactor, m_FMRIPositiveCalMinFactor, m_FMRIPositiveCalMaxFactor, m_LocalizersMin, m_LocalizersMiddle, m_LocalizersMax, m_Scene.Visualization.Configuration.AtlasConfiguration?.ID);
        }

        public void LoadConfiguration(AtlasConfiguration configuration)
        {
            if (configuration == null) return; // Legacy visualizations retain their normal defaults.
            m_Scene.AtlasManager.AtlasAlpha = configuration.AtlasAlpha;
            m_Scene.AtlasManager.DisplayMarsAtlas = configuration.MarsAtlas;
            m_Scene.AtlasManager.DisplayJuBrainAtlas = configuration.JuBrain;
            // Assign the complete resource selection before notifying dependent renderers.
            m_SelectedIBCContrastID = configuration.IBCIndex;
            m_SelectedDiFuMoAtlas = configuration.DiFuMoAtlas;
            m_SelectedDiFuMoArea = configuration.DiFuMoArea;
            m_SelectedLocalizersProtocol = configuration.LocalizerProtocol;
            m_SelectedLocalizersData = configuration.LocalizerData;
            m_SelectedLocalizersBloc = configuration.LocalizerBloc;
            m_SelectedLocalizersTimelineIndex = configuration.LocalizerTime;
            m_FMRIAlpha = configuration.FMRIAlpha;
            m_FMRINegativeCalMinFactor = configuration.NegativeMin;
            m_FMRINegativeCalMaxFactor = configuration.NegativeMax;
            m_FMRIPositiveCalMinFactor = configuration.PositiveMin;
            m_FMRIPositiveCalMaxFactor = configuration.PositiveMax;
            m_LocalizersMin = configuration.LocalizerMin;
            m_LocalizersMiddle = configuration.LocalizerMiddle;
            m_LocalizersMax = configuration.LocalizerMax;
            bool supportsMNI = m_Scene.MeshManager.SelectedMesh.SupportsMNIResources;
            m_DisplayIBCContrasts = configuration.IBC && supportsMNI;
            m_DisplayDiFuMo = configuration.DiFuMo && supportsMNI;
            m_DisplayLocalizers = configuration.Localizers && supportsMNI;
            UpdateSurfaceFMRIValues();
            UpdateSurfaceFMRIColors();
        }

        /// <summary>Apply prepared IBC and DiFuMo selections before recomputing their surface output.</summary>
        public void ApplySynchronizedAtlasSources(bool ibc, int ibcContrast, bool difumo, string difumoAtlas, int difumoArea)
        {
            if (m_DisplayIBCContrasts == ibc && m_SelectedIBCContrastID == ibcContrast && m_DisplayDiFuMo == difumo && m_SelectedDiFuMoAtlas == difumoAtlas && m_SelectedDiFuMoArea == difumoArea) return;
            m_SelectedIBCContrastID = ibcContrast;
            m_SelectedDiFuMoAtlas = difumoAtlas;
            m_SelectedDiFuMoArea = difumoArea;
            m_DisplayIBCContrasts = ibc;
            m_DisplayDiFuMo = difumo;
            UpdateSurfaceFMRIValues();
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        /// <summary>Apply the prepared localizer selection and thresholds with one surface update.</summary>
        public void ApplySynchronizedLocalizer(bool enabled, string protocol, string data, string bloc, int timelineIndex, float min, float middle, float max)
        {
            if (m_DisplayLocalizers == enabled && m_SelectedLocalizersProtocol == protocol && m_SelectedLocalizersData == data && m_SelectedLocalizersBloc == bloc && m_SelectedLocalizersTimelineIndex == timelineIndex && m_LocalizersMin == min && m_LocalizersMiddle == middle && m_LocalizersMax == max) return;
            m_DisplayLocalizers = enabled;
            m_SelectedLocalizersProtocol = protocol;
            m_SelectedLocalizersData = data;
            m_SelectedLocalizersBloc = bloc;
            m_SelectedLocalizersTimelineIndex = timelineIndex;
            m_LocalizersMin = min;
            m_LocalizersMiddle = middle;
            m_LocalizersMax = max;
            UpdateSurfaceFMRIValues();
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

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
            get { return m_DisplayIBCContrasts; }
            set
            {
                m_DisplayIBCContrasts = value && m_Scene.MeshManager.SelectedMesh.SupportsMNIResources;
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
            get { return m_SelectedIBCContrastID; }
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
            get { return m_DisplayDiFuMo; }
            set
            {
                m_DisplayDiFuMo = value && m_Scene.MeshManager.SelectedMesh.SupportsMNIResources;
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
            get { return m_SelectedDiFuMoArea; }
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
            get { return m_DisplayLocalizers; }
            set
            {
                m_DisplayLocalizers = value && m_Scene.MeshManager.SelectedMesh.SupportsMNIResources;
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
                SelectedLocalizersData = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == m_SelectedLocalizersProtocol)?.Datas.FirstOrDefault()?.Name;
                SetLocalizersDefaultParameters();
            }
        }

        private string m_SelectedLocalizersData;

        /// <summary>
        /// Currently selected Localizers data
        /// </summary>
        public string SelectedLocalizersData
        {
            get
            {
                if (string.IsNullOrEmpty(m_SelectedLocalizersData))
                {
                    m_SelectedLocalizersData = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == m_SelectedLocalizersProtocol)?.Datas.FirstOrDefault()?.Name;
                }

                return m_SelectedLocalizersData;
            }
            set
            {
                m_SelectedLocalizersData = value;
                var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == m_SelectedLocalizersProtocol);
                var data = protocol?.Datas.FirstOrDefault(d => d.Name == m_SelectedLocalizersData);
                SelectedLocalizersBloc = data?.Blocs.FirstOrDefault()?.Name;
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
                    var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == m_SelectedLocalizersProtocol);
                    var data = protocol?.Datas.FirstOrDefault(d => d.Name == m_SelectedLocalizersData);
                    m_SelectedLocalizersBloc = data?.Blocs.FirstOrDefault()?.Name;
                }

                return m_SelectedLocalizersBloc;
            }
            set
            {
                m_SelectedLocalizersBloc = value;
                SelectedLocalizersTimelineIndex = m_SelectedLocalizersTimelineIndex;
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
                var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedLocalizersProtocol, SelectedLocalizersData, SelectedLocalizersBloc);
                if (currentFMRI != null && currentFMRI.Volumes.Count > 0)
                {
                    return Mathf.Clamp(m_SelectedLocalizersTimelineIndex, 0, currentFMRI.Volumes.Count - 1);
                }

                return 0;
            }
            set
            {
                var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedLocalizersProtocol, SelectedLocalizersData, SelectedLocalizersBloc);
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

        public Core.Object3D.FMRI CurrentFMRI
        {
            get
            {
                if (m_DisplayIBCContrasts)
                {
                    return Object3DManager.IBC.FMRI;
                }
                else if (m_DisplayDiFuMo)
                {
                    return Object3DManager.DiFuMo.FMRIs[m_SelectedDiFuMoAtlas];
                }
                else if (m_DisplayLocalizers)
                {
                    return Object3DManager.Localizers.GetCurrentFMRI(SelectedLocalizersProtocol, SelectedLocalizersData, SelectedLocalizersBloc);
                }

                return null;
            }
        }

        /// <summary>
        /// Currently used volume (depends on the type of fMRI we are displaying)
        /// </summary>
        public Volume CurrentVolume
        {
            get
            {
                if (m_DisplayIBCContrasts)
                {
                    return CurrentFMRI.Volumes[m_SelectedIBCContrastID];
                }
                else if (m_DisplayDiFuMo)
                {
                    return CurrentFMRI.Volumes[m_SelectedDiFuMoArea];
                }
                else if (m_DisplayLocalizers)
                {
                    return CurrentFMRI.Volumes[m_SelectedLocalizersTimelineIndex];
                }

                return null;
            }
        }

        /// <summary>
        /// Do we display a FMRI ?
        /// </summary>
        public bool DisplayFMRI
        {
            get { return CurrentVolume != null; }
        }

        private float[] m_FMRIValues;
        private int[] m_FMRIMask;

        private float m_FMRIAlpha = 0.2f;

        /// <summary>
        /// Alpha of the FMRI
        /// </summary>
        public float FMRIAlpha
        {
            get { return m_FMRIAlpha; }
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
            get { return m_FMRINegativeCalMinFactor; }
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
            get { return m_FMRINegativeCalMaxFactor; }
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
            get { return m_FMRIPositiveCalMinFactor; }
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
            get { return m_FMRIPositiveCalMaxFactor; }
            set
            {
                m_FMRIPositiveCalMaxFactor = value;
                UpdateSurfaceFMRIColors();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        /// <summary>Apply synchronized atlas display calibration with one color rebuild.</summary>
        public void ApplySynchronizedAtlasCalibration(float alpha, float negativeMin, float negativeMax, float positiveMin, float positiveMax)
        {
            if (m_FMRIAlpha == alpha && m_FMRINegativeCalMinFactor == negativeMin && m_FMRINegativeCalMaxFactor == negativeMax && m_FMRIPositiveCalMinFactor == positiveMin && m_FMRIPositiveCalMaxFactor == positiveMax) return;
            m_FMRIAlpha = alpha;
            m_FMRINegativeCalMinFactor = negativeMin;
            m_FMRINegativeCalMaxFactor = negativeMax;
            m_FMRIPositiveCalMinFactor = positiveMin;
            m_FMRIPositiveCalMaxFactor = positiveMax;
            UpdateSurfaceFMRIColors();
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        private const float m_DiFuMoNegativeMin = 0;
        private const float m_DiFuMoNegativeMax = 1;
        private const float m_DiFuMoPositiveMin = 0;
        private const float m_DiFuMoPositiveMax = 1;
        private const float m_DiFuMoAlpha = 1f;

        private readonly Color32[] m_LocalizersColorSchemePixels = UnityTextureFactory.Generate1DColorPixels(Core.Enums.ColorType.MatLab);

        private float m_LocalizersMin = 80f;

        /// <summary>
        /// Min value for Localizers threshold
        /// </summary>
        public float LocalizersMin
        {
            get { return m_LocalizersMin; }
            set
            {
                m_LocalizersMin = value;
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_LocalizersMiddle = 100f;

        /// <summary>
        /// Middle value for Localizers threshold
        /// </summary>
        public float LocalizersMiddle
        {
            get { return m_LocalizersMiddle; }
            set
            {
                m_LocalizersMiddle = value;
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        private float m_LocalizersMax = 120f;

        /// <summary>
        /// Max value for Localizers threshold
        /// </summary>
        public float LocalizersMax
        {
            get { return m_LocalizersMax; }
            set
            {
                m_LocalizersMax = value;
                UpdateSurfaceFMRIValues();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
        }

        /// <summary>
        /// Currently used mask volume for Localizers (if available)
        /// </summary>
        public Volume CurrentLocalizersMask
        {
            get
            {
                if (m_DisplayLocalizers)
                {
                    var currentFMRI = Object3DManager.Localizers.GetCurrentFMRI(SelectedLocalizersProtocol, SelectedLocalizersData, SelectedLocalizersBloc);
                    return currentFMRI?.MaskVolume;
                }

                return null;
            }
        }

        #endregion

        #region Public Methods

        public void UpdateSurfaceFMRIValues()
        {
            m_Scene.BrainMaterials.SetDisplayFMRI(DisplayFMRI);
            if (CurrentVolume != null)
                m_FMRIValues = CurrentVolume.GetVerticesValues(m_Scene.MeshManager.ReferenceSurface);
            if (CurrentLocalizersMask != null)
                m_FMRIMask = CurrentLocalizersMask.GetVerticesValues(m_Scene.MeshManager.ReferenceSurface).Select(v => v > 0 ? 1 : 0).ToArray();

            UpdateSurfaceFMRIColors();
        }

        /// <summary>
        /// Update all colors for the FMRI for all vertices
        /// </summary>
        public void UpdateSurfaceFMRIColors()
        {
            if (!DisplayFMRI && m_Scene.AtlasManager.DisplayAtlas)
            {
                m_Scene.AtlasManager.UpdateAtlasColors();
                return;
            }

            Color[] colors = new Color[0];
            if (m_DisplayIBCContrasts)
            {
                colors = CurrentVolume.ConvertValuesToColors(m_FMRIValues, m_FMRINegativeCalMinFactor, m_FMRINegativeCalMaxFactor, m_FMRIPositiveCalMinFactor, m_FMRIPositiveCalMaxFactor, m_FMRIAlpha);
            }
            else if (m_DisplayDiFuMo)
            {
                colors = CurrentVolume.ConvertValuesToColors(m_FMRIValues, m_DiFuMoNegativeMin, m_DiFuMoNegativeMax, m_DiFuMoPositiveMin, m_DiFuMoPositiveMax, m_DiFuMoAlpha);
            }
            else if (m_DisplayLocalizers)
            {
                colors = CurrentVolume.ConvertValuesToColors(m_FMRIValues, m_FMRIMask, m_LocalizersMin, m_LocalizersMiddle, m_LocalizersMax, m_LocalizersColorSchemePixels);
            }

            m_DisplayedObjects.Brain.GetComponent<MeshFilter>().sharedMesh.colors = colors;
            foreach (Column3D column in m_Scene.Columns)
            {
                column.BrainMesh.GetComponent<MeshFilter>().sharedMesh.colors = colors;
            }

            m_Scene.SceneInformation.BaseCutTexturesNeedUpdate = true;
        }

        public void ColorCuts(Column3D column)
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
                column.CutTextures.ColorCutsTexturesWithLocalizersAtlas(CurrentVolume, m_LocalizersMin, m_LocalizersMiddle, m_LocalizersMax, CurrentLocalizersMask, m_LocalizersColorSchemePixels);
            }
        }

        public void SetLocalizersDefaultParameters()
        {
            if (m_DisplayLocalizers)
            {
                var currentFMRI = CurrentFMRI;
                if (currentFMRI != null)
                {
                    LocalizersMin = currentFMRI.ExtremeValues.ComputedCalMin;
                    LocalizersMiddle = (currentFMRI.ExtremeValues.ComputedCalMin + currentFMRI.ExtremeValues.ComputedCalMax) / 2f;
                    LocalizersMax = currentFMRI.ExtremeValues.ComputedCalMax;
                }
            }
            else
            {
                LocalizersMin = 80f;
                LocalizersMiddle = 100f;
                LocalizersMax = 120f;
            }
        }

        #endregion
    }
}
