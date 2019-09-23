using System.Collections.Generic;
using System.Linq;

namespace HBP.Module3D
{
    /// <summary>
    /// This class contains information about an Implantation and can load implantation files to DLL objects
    /// </summary>
    public class Implantation3D
    {
        #region Properties
        /// <summary>
        /// Name of the implantation
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// List of the sites in the DLL
        /// </summary>
        public DLL.PatientElectrodesList PatientElectrodesList { get; set; } = new DLL.PatientElectrodesList();
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
        public Implantation3D(string name, IEnumerable<string> pts, IEnumerable<string> marsAtlas, IEnumerable<string> patientIDs)
        {
            Name = name;
            IsLoaded = PatientElectrodesList.LoadPTSFiles(pts.ToArray(), marsAtlas.ToArray(), patientIDs.ToArray(), ApplicationState.Module3D.MarsAtlasIndex);
            PatientElectrodesList.ExtractRawSiteList(RawSiteList);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Dispose all the DLL objects
        /// </summary>
        public void Clean()
        {
            PatientElectrodesList?.Dispose();
            RawSiteList?.Dispose();
        }
        #endregion
    }
}