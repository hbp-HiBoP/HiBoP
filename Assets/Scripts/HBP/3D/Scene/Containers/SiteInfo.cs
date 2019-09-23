using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Site info to be send to the UI
    /// </summary>
    public class SiteInfo
    {
        public SiteInfo(Site site, bool enabled, Vector3 position, Data.Enums.SiteInformationDisplayMode mode = Data.Enums.SiteInformationDisplayMode.IEEG, string ieegAmplitude = "", string ieegUnit = "", string ccepAmplitude = "", string ccepLatency = "")
        {
            Site = site;
            Enabled = enabled;
            Position = position;
            IEEGAmplitude = ieegAmplitude;
            IEEGUnit = ieegUnit;
            CCEPAmplitude = ccepAmplitude;
            CCEPLatency = ccepLatency;
            Mode = mode;
        }

        public Site Site { get; set; }
        public bool Enabled { get; set; }
        public Vector3 Position { get; set; }
        public string IEEGAmplitude { get; set; }
        public string IEEGUnit { get; set; }
        public string CCEPAmplitude { get; set; }
        public string CCEPLatency { get; set; }
        public Data.Enums.SiteInformationDisplayMode Mode { get; set; }
    }

}