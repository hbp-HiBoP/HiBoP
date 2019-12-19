using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Small class containing information about a site
    /// This is used to get information and transfer it to the UI when hovering a site with the mouse
    /// </summary>
    public class SiteInfo
    {
        #region Properties
        /// <summary>
        /// Site currently being hovered
        /// </summary>
        public Site Site { get; set; }
        /// <summary>
        /// Do we display information about this site ?
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Position where to display information
        /// </summary>
        public Vector3 Position { get; set; }
        /// <summary>
        /// iEEG amplitude of the site
        /// </summary>
        public string IEEGAmplitude { get; set; }
        /// <summary>
        /// Unit used for the activity of the site
        /// </summary>
        public string IEEGUnit { get; set; }
        /// <summary>
        /// Amplitude of the first spike of this site
        /// </summary>
        public string CCEPAmplitude { get; set; }
        /// <summary>
        /// Latency of the first spike of this site
        /// </summary>
        public string CCEPLatency { get; set; }
        /// <summary>
        /// How to display the information
        /// </summary>
        public Data.Enums.SiteInformationDisplayMode Mode { get; set; }
        #endregion

        #region Constructor
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
        #endregion
    }
}