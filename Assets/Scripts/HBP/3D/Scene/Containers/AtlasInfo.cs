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
        public string Name { get; set; }
        /// <summary>
        /// Precise location of the area within the brain
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// Label of the area (as described in the json file)
        /// </summary>
        public string AreaLabel { get; set; }
        /// <summary>
        /// Status of this area
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// DOI of the area
        /// </summary>
        public string DOI { get; set; }
        #endregion

        #region Constructor
        public AtlasInfo(bool enabled, Vector3 position, string name = "", string location = "", string areaLabel = "", string status = "", string doi = "")
        {
            Enabled = enabled;
            Position = position;
            Name = name;
            Location = location;
            AreaLabel = areaLabel;
            Status = status;
            DOI = doi;
        }
        #endregion
    }
}