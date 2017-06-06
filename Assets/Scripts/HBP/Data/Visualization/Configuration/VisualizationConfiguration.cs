using UnityEngine;
using System.Runtime.Serialization;

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
    }
}