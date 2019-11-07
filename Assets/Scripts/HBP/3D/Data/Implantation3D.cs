using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// This class contains information about an Implantation and can load implantation files to DLL objects
    /// </summary>
    public class Implantation3D
    {
        #region Structs
        /// <summary>
        /// Struct containing basic information about a site
        /// </summary>
        public class SiteInfo
        {
            /// <summary>
            /// Name of the site
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Position of the site
            /// </summary>
            public Vector3 Position { get; set; }
            /// <summary>
            /// Index of this site within the electrode
            /// </summary>
            public int Index { get; set; }
            /// <summary>
            /// Position of the site in the Unity reference
            /// </summary>
            public Vector3 UnityPosition
            {
                get
                {
                    return new Vector3(-Position.x, Position.y, Position.z);
                }
            }
            /// <summary>
            /// Index of the patient in the visualization
            /// </summary>
            public int PatientIndex { get; set; }
            /// <summary>
            /// Patient this site belongs to
            /// </summary>
            public Data.Patient Patient { get; set; }
            /// <summary>
            /// Reference to the site data
            /// </summary>
            public Data.Anatomy.Site SiteData { get; set; }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Name of the implantation
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// List of the sites of this implantation
        /// </summary>
        public List<SiteInfo> SiteInfos { get; private set; }
        /// <summary>
        /// Raw list of all sites
        /// </summary>
        public DLL.RawSiteList RawSiteList { get; set; } = new DLL.RawSiteList();
        /// <summary>
        /// Is the implantation loaded ?
        /// </summary>
        public bool IsLoaded { get; set; }
        #endregion

        #region Constructors
        public Implantation3D(string name, List<SiteInfo> siteInfos, IEnumerable<Data.Patient> patients)
        {
            Name = name;
            SiteInfos = siteInfos;
            RawSiteList.SetPatients(patients);
            foreach (var siteInfo in siteInfos)
            {
                RawSiteList.AddSite(siteInfo.Name, siteInfo.Position, siteInfo.PatientIndex, siteInfo.Index);
            }
            IsLoaded = true;
        }
        #endregion

        #region Public Methods
        public IEnumerable<SiteInfo> GetSitesOfPatient(Data.Patient patient)
        {
            return SiteInfos.Where(s => s.Patient == patient);
        }
        /// <summary>
        /// Dispose all the DLL objects
        /// </summary>
        public void Clean()
        {
            RawSiteList?.Dispose();
        }
        #endregion
    }
}