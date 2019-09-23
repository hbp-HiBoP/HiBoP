using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    public class AtlasInfo
    {
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

        public bool Enabled { get; set; }
        public Vector3 Position { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string AreaLabel { get; set; }
        public string Status { get; set; }
        public string DOI { get; set; }
    }
}