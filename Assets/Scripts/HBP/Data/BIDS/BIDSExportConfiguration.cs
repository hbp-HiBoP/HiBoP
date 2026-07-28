using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace HBP.Data.BIDS
{
    /// <summary>
    /// Configuration for BIDS export that defines rules for anatomical data and coordinate systems.
    /// Can be loaded from/saved to JSON files for easy customization.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class BIDSExportConfiguration
    {
        /// <summary>
        /// Rules for exporting anatomical data (MRIs and Meshes).
        /// </summary>
        [JsonProperty] public List<AnatomicalDataRule> AnatomicalRules { get; set; } = new List<AnatomicalDataRule>();

        /// <summary>
        /// Rules for exporting coordinate systems.
        /// </summary>
        [JsonProperty] public List<CoordinateSystemRule> CoordinateSystemRules { get; set; } = new List<CoordinateSystemRule>();

        /// <summary>
        /// Configuration version for future compatibility.
        /// </summary>
        [JsonProperty] public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Rule for exporting anatomical data (MRI or Mesh).
    /// </summary>
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class AnatomicalDataRule
    {
        /// <summary>
        /// Type of data to export. Valid values: "MRI" or "Mesh".
        /// </summary>
        [JsonProperty] public string DataType { get; set; }

        /// <summary>
        /// Name to match in Patient.MRIs or Patient.Meshes (e.g., "Preimplantation", "Grey matter").
        /// </summary>
        [JsonProperty] public string SourceName { get; set; }

        /// <summary>
        /// BIDS suffix to use in the filename (e.g., "T1w", "T2w", "CT", "pial", "white").
        /// </summary>
        [JsonProperty] public string BIDSSuffix { get; set; }

        /// <summary>
        /// BIDS session name (e.g., "pre", "post").
        /// </summary>
        [JsonProperty] public string BIDSSession { get; set; }
    }

    /// <summary>
    /// Rule for exporting coordinate systems.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class CoordinateSystemRule
    {
        /// <summary>
        /// Name to match in Site.Coordinates[].ReferenceSystem (e.g., "Patient", "MNI").
        /// </summary>
        [JsonProperty] public string CoordinateSystemName { get; set; }

        /// <summary>
        /// BIDS space entity value. Use empty string for scanner space, or "MNI152Lin", etc.
        /// </summary>
        [JsonProperty] public string BIDSSpace { get; set; }
    }
}
