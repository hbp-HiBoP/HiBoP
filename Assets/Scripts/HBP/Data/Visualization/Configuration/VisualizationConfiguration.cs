using UnityEngine;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class VisualizationConfiguration
    {
        [DataMember(Name = "Color")]
        SerializableColor color;
        /// <summary>
        /// Color of the visualization.
        /// </summary>
        public Color Color { get; set; }

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
    }
}