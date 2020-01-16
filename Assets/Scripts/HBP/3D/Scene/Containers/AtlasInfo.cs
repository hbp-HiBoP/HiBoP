using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Small class containing information about an area of the JuBrain Atlas
    /// </summary>
    /// <remarks>
    /// This is used to get information and transfer it to the UI when hovering an area with the mouse <seealso cref="DLL.JuBrainAtlas"/>
    /// </remarks>
    public class AtlasInfo
    {
        #region Properties
        public enum AtlasType { MarsAtlas, JuBrainAtlas }
        public AtlasType Type;
        /// <summary>
        /// Do we display information about this area ?
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Position where to display information
        /// </summary>
        public Vector3 Position { get; set; }
        /// <summary>
        /// Name of the area
        /// </summary>
        public string Information1 { get; set; }
        /// <summary>
        /// Precise location of the area within the brain
        /// </summary>
        public string Information2 { get; set; }
        /// <summary>
        /// Label of the area (as described in the json file)
        /// </summary>
        public string Information3 { get; set; }
        /// <summary>
        /// Status of this area
        /// </summary>
        public string Information4 { get; set; }
        /// <summary>
        /// DOI of the area
        /// </summary>
        public string Information5 { get; set; }
        #endregion

        #region Constructor
        public AtlasInfo(bool enabled, Vector3 position, AtlasType type = AtlasType.MarsAtlas, string name = "", string location = "", string areaLabel = "", string status = "", string doi = "")
        {
            Enabled = enabled;
            Position = position;
            Type = type;
            Information1 = name;
            Information2 = location;
            Information3 = areaLabel;
            Information4 = status;
            Information5 = doi;
        }
        #endregion
    }
}