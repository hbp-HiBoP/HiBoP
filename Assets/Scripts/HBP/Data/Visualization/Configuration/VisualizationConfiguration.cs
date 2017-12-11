using UnityEngine;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class VisualizationConfiguration
    {
        [DataMember(Name = "Brain Color")]
        private ColorType m_BrainColor = ColorType.BrainColor;
        /// <summary>
        /// Color of the brain
        /// </summary>
        public ColorType BrainColor
        {
            get
            {
                return m_BrainColor;
            }
            set
            {
                m_BrainColor = value;
            }
        }

        [DataMember(Name = "Brain Cut Color")]
        private ColorType m_BrainCutColor = ColorType.Default;
        /// <summary>
        /// Color of the brain
        /// </summary>
        public ColorType BrainCutColor
        {
            get
            {
                return m_BrainCutColor;
            }
            set
            {
                m_BrainCutColor = value;
            }
        }

        [DataMember(Name = "Colormap")]
        private ColorType m_Colormap = ColorType.MatLab;
        /// <summary>
        /// Color of the brain
        /// </summary>
        public ColorType Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                m_Colormap = value;
            }
        }

        [DataMember(Name = "Mesh Part")]
        private Module3D.SceneStatesInfo.MeshPart m_MeshPart = Module3D.SceneStatesInfo.MeshPart.Both;
        /// <summary>
        /// Mesh part to display
        /// </summary>
        public Module3D.SceneStatesInfo.MeshPart MeshPart
        {
            get
            {
                return m_MeshPart;
            }
            set
            {
                m_MeshPart = value;
            }
        }

        [DataMember(Name = "Mesh")]
        private string m_MeshName = "";
        /// <summary>
        /// Mesh part to display
        /// </summary>
        public string MeshName
        {
            get
            {
                return m_MeshName;
            }
            set
            {
                m_MeshName = value;
            }
        }

        [DataMember(Name = "MRI")]
        private string m_MRIName = "";
        /// <summary>
        /// Mesh part to display
        /// </summary>
        public string MRIName
        {
            get
            {
                return m_MRIName;
            }
            set
            {
                m_MRIName = value;
            }
        }

        [DataMember(Name = "Implantation")]
        private string m_ImplantationName = "";
        /// <summary>
        /// Mesh part to display
        /// </summary>
        public string ImplantationName
        {
            get
            {
                return m_ImplantationName;
            }
            set
            {
                m_ImplantationName = value;
            }
        }

        [DataMember(Name = "Edges")]
        private bool m_EdgeMode = false;
        /// <summary>
        /// Show edges of the meshes
        /// </summary>
        public bool EdgeMode
        {
            get
            {
                return m_EdgeMode;
            }
            set
            {
                m_EdgeMode = value;
            }
        }

        [DataMember(Name = "Sites")]
        private bool m_ShowAllSites = false;
        /// <summary>
        /// Show all sites in the scene
        /// </summary>
        public bool ShowAllSites
        {
            get
            {
                return m_ShowAllSites;
            }
            set
            {
                m_ShowAllSites = value;
            }
        }

        [DataMember(Name = "MRI Min")]
        private float m_MRICalMinFactor = 0.0f;
        /// <summary>
        /// MRI Cal Min Factor
        /// </summary>
        public float MRICalMinFactor
        {
            get
            {
                return m_MRICalMinFactor;
            }
            set
            {
                m_MRICalMinFactor = value;
            }
        }

        [DataMember(Name = "MRI Max")]
        private float m_MRICalMaxFactor = 1.0f;
        /// <summary>
        /// MRI Cal Max Factor
        /// </summary>
        public float MRICalMaxFactor
        {
            get
            {
                return m_MRICalMaxFactor;
            }
            set
            {
                m_MRICalMaxFactor = value;
            }
        }

        [DataMember(Name = "Camera Type")]
        private Module3D.CameraControl m_CameraType = Module3D.CameraControl.Trackball;
        /// <summary>
        /// Camera control type
        /// </summary>
        public Module3D.CameraControl CameraType
        {
            get
            {
                return m_CameraType;
            }
            set
            {
                m_CameraType = value;
            }
        }

        [DataMember(Name = "Cuts")]
        private List<Cut> m_Cuts = new List<Cut>();
        /// <summary>
        /// Cuts of the visualization
        /// </summary>
        public List<Cut> Cuts
        {
            get
            {
                return m_Cuts;
            }
            set
            {
                m_Cuts = value;
            }
        }

        [DataMember(Name = "Views")]
        private List<View> m_Views = new List<View>();
        /// <summary>
        /// Views of the visualization
        /// </summary>
        public List<View> Views
        {
            get
            {
                return m_Views;
            }
            set
            {
                m_Views = value;
            }
        }

        public VisualizationConfiguration Clone()
        {
            VisualizationConfiguration configuration = new VisualizationConfiguration();
            configuration.BrainColor = BrainColor;
            configuration.BrainCutColor = BrainCutColor;
            configuration.Colormap = Colormap;
            configuration.MeshPart = MeshPart;
            configuration.MeshName = MeshName;
            configuration.MRIName = MRIName;
            configuration.ImplantationName = ImplantationName;
            configuration.EdgeMode = EdgeMode;
            configuration.ShowAllSites = ShowAllSites;
            configuration.MRICalMinFactor = MRICalMinFactor;
            configuration.MRICalMaxFactor = MRICalMaxFactor;
            configuration.CameraType = CameraType;
            configuration.Cuts = new List<Cut>();
            foreach (Cut cut in Cuts)
            {
                configuration.Cuts.Add(new Cut(cut.Normal.ToVector3(), cut.Orientation, cut.Flip, cut.Position));
            }
            configuration.Views = new List<View>();
            foreach (View view in Views)
            {
                configuration.Views.Add(new View(view.Position.ToVector3(), view.Rotation.ToQuaternion(), view.Target.ToVector3()));
            }
            return configuration;
        }
    }
}