using System.Collections.Generic;
using HBP.Core.Enums;
using System.Linq;
using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class VisualizationConfiguration : BaseData
    {
        #region Properties
        /// <summary>
        /// Color of the brain
        /// </summary>
        [JsonProperty("Brain Color")]
        public ColorType BrainColor { get; set; } = ColorType.BrainColor;

        /// <summary>
        /// Color of the cuts
        /// </summary>
        [JsonProperty("Brain Cut Color")]
        public ColorType BrainCutColor { get; set; } = ColorType.Default;

        /// <summary>
        /// EEG colormap
        /// </summary>
        [JsonProperty("Colormap")]
        public ColorType Colormap { get; set; } = ColorType.MatLab;

        /// <summary>
        /// Mesh part to display
        /// </summary>
        [JsonProperty("Mesh Part")]
        public MeshPart MeshPart { get; set; } = MeshPart.Both;

        /// <summary>
        /// Mesh to display
        /// </summary>
        [JsonProperty("Mesh")]
        public string MeshName { get; set; }

        /// <summary>
        /// MRI to display
        /// </summary>
        [JsonProperty("MRI")]
        public string MRIName { get; set; }

        /// <summary>
        /// Implantation to display
        /// </summary>
        [JsonProperty("Implantation")]
        public string ImplantationName { get; set; }

        /// <summary>
        /// Show edges of the meshes
        /// </summary>
        [JsonProperty("Edges")]
        public bool ShowEdges { get; set; }

        /// <summary>
        /// Is the mesh invisible?
        /// </summary>
        [JsonProperty("Transparent Brain")]
        public bool TransparentBrain { get; set; }

        /// <summary>
        /// Alpha value when the brain is invisible
        /// </summary>
        [JsonProperty("Brain Alpha")]
        public float BrainAlpha { get; set; } = 0.2f;

        /// <summary>
        /// Cut behaviour
        /// </summary>
        [JsonProperty("Strong Cuts")]
        public bool StrongCuts { get; set; }

        /// <summary>
        /// Hide blacklisted sites
        /// </summary>
        [JsonProperty("Hide Blacklisted Sites")]
        public bool HideBlacklistedSites { get; set; }

        /// <summary>
        /// Show all sites in the scene
        /// </summary>
        [JsonProperty("ShowAllSites")]
        public bool ShowAllSites { get; set; }

        /// <summary>
        /// Automatically cut the brain around the selected site
        /// </summary>
        [JsonProperty("AutomaticCutAroundSelectedSite")]
        public bool AutomaticCutAroundSelectedSite { get; set; } = false;

        /// <summary>
        /// Sites Gain
        /// </summary>
        [JsonProperty("Site Gain")]
        public float SiteGain { get; set; } = 1.0f;

        /// <summary>
        /// MRI Cal Min Factor
        /// </summary>
        [JsonProperty("MRI Min")]
        public float MRICalMinFactor { get; set; }

        /// <summary>
        /// MRI Cal Max Factor
        /// </summary>
        [JsonProperty("MRI Max")]
        public float MRICalMaxFactor { get; set; } = 1;

        /// <summary>
        /// Camera control type
        /// </summary>
        [JsonProperty("Camera Type")]
        public CameraControl CameraType { get; set; } = CameraControl.Trackball;

        /// <summary>
        /// Cuts of the visualization
        /// </summary>
        [JsonProperty("Cuts")]
        public List<Cut> Cuts { get; set; } = new List<Cut>();

        /// <summary>
        /// Views of the visualization
        /// </summary>
        [JsonProperty("Views")]
        public List<View> Views { get; set; } = new List<View>();

        /// <summary>
        /// Region of interest.
        /// </summary>
        [JsonProperty] public List<RegionOfInterest> RegionsOfInterest { get; set; } = new List<RegionOfInterest>();

        [JsonIgnore]
        public string FirstSiteToSelect { get; set; }
        [JsonIgnore]
        public int FirstColumnToSelect { get; set; }
        [JsonIgnore]
        public List<Object3D.Mesh3D> PreloadedMeshes { get; set; } = new List<Object3D.Mesh3D>();
        [JsonIgnore]
        public List<Object3D.MRI3D> PreloadedMRIs { get; set; } = new List<Object3D.MRI3D>();
        #endregion

        #region Constructors
        public VisualizationConfiguration(ColorType brainColor, ColorType brainCutColor, ColorType eEGColormap, MeshPart meshPart, string meshName, string mRIName, string implantationName, bool showEdges, bool transparent, float alpha, bool strongCuts, bool hideBlacklistedSites, bool showAllSites, bool automaticCutAroundSelectedSite, float siteGain, float mRICalMinFactor, float mRICalMaxFactor, CameraControl cameraType, IEnumerable<Cut> cuts, IEnumerable<View> views, IEnumerable<RegionOfInterest> rois) : base()
        {
            BrainColor = brainColor;
            BrainCutColor = brainCutColor;
            Colormap = eEGColormap;
            MeshPart = meshPart;
            MeshName = meshName;
            MRIName = mRIName;
            ImplantationName = implantationName;
            ShowEdges = showEdges;
            TransparentBrain = transparent;
            BrainAlpha = alpha;
            StrongCuts = strongCuts;
            HideBlacklistedSites = hideBlacklistedSites;
            ShowAllSites = showAllSites;
            AutomaticCutAroundSelectedSite = automaticCutAroundSelectedSite;
            SiteGain = siteGain;
            MRICalMinFactor = mRICalMinFactor;
            MRICalMaxFactor = mRICalMaxFactor;
            CameraType = cameraType;
            Cuts = cuts.ToList();
            Views = views.ToList();
            RegionsOfInterest = rois.ToList();
        }
        public VisualizationConfiguration(ColorType brainColor, ColorType brainCutColor, ColorType eEGColormap, MeshPart meshPart, string meshName, string mRIName, string implantationName, bool showEdges, bool transparent, float alpha, bool strongCuts, bool hideBlacklistedSites, bool showAllSites, bool automaticCutAroundSelectedSite, float siteGain, float mRICalMinFactor, float mRICalMaxFactor, CameraControl cameraType, IEnumerable<Cut> cuts, IEnumerable<View> views, IEnumerable<RegionOfInterest> rois, string ID) : base(ID)
        {
            BrainColor = brainColor;
            BrainCutColor = brainCutColor;
            Colormap = eEGColormap;
            MeshPart = meshPart;
            MeshName = meshName;
            MRIName = mRIName;
            ImplantationName = implantationName;
            ShowEdges = showEdges;
            TransparentBrain = transparent;
            BrainAlpha = alpha;
            StrongCuts = strongCuts;
            HideBlacklistedSites = hideBlacklistedSites;
            ShowAllSites = showAllSites;
            AutomaticCutAroundSelectedSite = automaticCutAroundSelectedSite;
            SiteGain = siteGain;
            MRICalMinFactor = mRICalMinFactor;
            MRICalMaxFactor = mRICalMaxFactor;
            CameraType = cameraType;
            Cuts = cuts.ToList();
            Views = views.ToList();
            RegionsOfInterest = rois.ToList();
        }
        public VisualizationConfiguration() : base()
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new VisualizationConfiguration(BrainColor, BrainCutColor, Colormap,
                MeshPart, MeshName, MRIName, ImplantationName, ShowEdges, TransparentBrain, BrainAlpha, StrongCuts,
                HideBlacklistedSites, ShowAllSites, AutomaticCutAroundSelectedSite, SiteGain, MRICalMinFactor,
                MRICalMaxFactor, CameraType, Cuts, Views, RegionsOfInterest, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is VisualizationConfiguration visualizationConfiguration)
            {
                BrainColor = visualizationConfiguration.BrainColor;
                BrainCutColor = visualizationConfiguration.BrainCutColor;
                Colormap = visualizationConfiguration.Colormap;
                MeshPart = visualizationConfiguration.MeshPart;
                MeshName = visualizationConfiguration.MeshName;
                MRIName = visualizationConfiguration.MRIName;
                ImplantationName = visualizationConfiguration.ImplantationName;
                ShowEdges = visualizationConfiguration.ShowEdges;
                TransparentBrain = visualizationConfiguration.TransparentBrain;
                BrainAlpha = visualizationConfiguration.BrainAlpha;
                StrongCuts = visualizationConfiguration.StrongCuts;
                HideBlacklistedSites = visualizationConfiguration.HideBlacklistedSites;
                ShowAllSites = visualizationConfiguration.ShowAllSites;
                AutomaticCutAroundSelectedSite = visualizationConfiguration.AutomaticCutAroundSelectedSite;
                SiteGain = visualizationConfiguration.SiteGain;
                MRICalMinFactor = visualizationConfiguration.MRICalMinFactor;
                MRICalMaxFactor = visualizationConfiguration.MRICalMaxFactor;
                CameraType = visualizationConfiguration.CameraType;
                Cuts = visualizationConfiguration.Cuts;
                Views = visualizationConfiguration.Views;
                RegionsOfInterest = visualizationConfiguration.RegionsOfInterest;
            }
        }
        #endregion
    }
}