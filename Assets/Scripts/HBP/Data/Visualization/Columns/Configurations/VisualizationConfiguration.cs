using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using HBP.Data.Enums;
using HBP.Module3D;
using System.Linq;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class VisualizationConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// Color of the brain
        /// </summary>
        [DataMember(Name = "Brain Color")]
        public ColorType BrainColor { get; set; } = ColorType.BrainColor;

        /// <summary>
        /// Color of the cuts
        /// </summary>
        [DataMember(Name = "Brain Cut Color")]
        public ColorType BrainCutColor { get; set; } = ColorType.Default;

        /// <summary>
        /// EEG colormap
        /// </summary>
        [DataMember(Name = "EEG Colormap")]
        public ColorType EEGColormap { get; set; } = ColorType.MatLab;

        /// <summary>
        /// Mesh part to display
        /// </summary>
        [DataMember(Name = "Mesh Part")]
        public MeshPart MeshPart { get; set; } = MeshPart.Both;

        /// <summary>
        /// Mesh to display
        /// </summary>
        [DataMember(Name = "Mesh")]
        public string MeshName { get; set; }

        /// <summary>
        /// MRI to display
        /// </summary>
        [DataMember(Name = "MRI")]
        public string MRIName { get; set; }

        /// <summary>
        /// Implantation to display
        /// </summary>
        [DataMember(Name = "Implantation")]
        public string ImplantationName { get; set; }

        /// <summary>
        /// Show edges of the meshes
        /// </summary>
        [DataMember(Name = "Edges")]
        public bool ShowEdges { get; set; }

        /// <summary>
        /// Cut behaviour
        /// </summary>
        [DataMember(Name = "Strong Cuts")]
        public bool StrongCuts { get; set; }

        /// <summary>
        /// Hide blacklisted sites
        /// </summary>
        [DataMember(Name = "Hide Blacklisted Sites")]
        public bool HideBlacklistedSites { get; set; }

        /// <summary>
        /// Show all sites in the scene
        /// </summary>
        [DataMember(Name = "ShowAllSites")]      
        public bool ShowAllSites { get; set; }

        /// <summary>
        /// MRI Cal Min Factor
        /// </summary>
        [DataMember(Name = "MRI Min")]
        public float MRICalMinFactor { get; set; }

        /// <summary>
        /// MRI Cal Max Factor
        /// </summary>
        [DataMember(Name = "MRI Max")]
        public float MRICalMaxFactor { get; set; } = 1;

        /// <summary>
        /// Camera control type
        /// </summary>
        [DataMember(Name = "Camera Type")]
        public CameraControl CameraType { get; set; } = CameraControl.Trackball;

        /// <summary>
        /// Cuts of the visualization
        /// </summary>
        [DataMember(Name = "Cuts")]
        public List<Cut> Cuts { get; set; } = new List<Cut>();

        /// <summary>
        /// Views of the visualization
        /// </summary>
        [DataMember(Name = "Views")]
        public List<View> Views { get; set; } = new List<View>();
        #endregion

        #region Constructors
        public VisualizationConfiguration(ColorType brainColor, ColorType brainCutColor, ColorType eEGColormap, MeshPart meshPart, string meshName, string mRIName, string implantationName, bool showEdges, bool strongCuts, bool hideBlacklistedSites, bool showAllSites, float mRICalMinFactor, float mRICalMaxFactor, CameraControl cameraType, List<Cut> cuts, List<View> views)
        {
            BrainColor = brainColor;
            BrainCutColor = brainCutColor;
            EEGColormap = eEGColormap;
            MeshPart = meshPart;
            MeshName = meshName;
            MRIName = mRIName;
            ImplantationName = implantationName;
            ShowEdges = showEdges;
            StrongCuts = strongCuts;
            HideBlacklistedSites = hideBlacklistedSites;
            ShowAllSites = showAllSites;
            MRICalMinFactor = mRICalMinFactor;
            MRICalMaxFactor = mRICalMaxFactor;
            CameraType = cameraType;
            Cuts = cuts;
            Views = views;
        }
        public VisualizationConfiguration()
        {
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new VisualizationConfiguration(BrainColor, BrainCutColor, EEGColormap,
                MeshPart, MeshName, MRIName, ImplantationName, ShowEdges, StrongCuts,
                HideBlacklistedSites, ShowAllSites, MRICalMinFactor,
                MRICalMaxFactor, CameraType, Cuts.ToList(), Views.ToList());
        }
        #endregion
    }
}