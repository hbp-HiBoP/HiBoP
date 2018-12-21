using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Information about an implantation
    /// </summary>
    public class Implantation3D
    {
        #region Properties
        /// <summary>
        /// Name of the implantation
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// List of the sites
        /// </summary>
        public DLL.PatientElectrodesList PatientElectrodesList { get; set; }
        /// <summary>
        /// Is the implantation loaded ?
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// Are the CCEP files loaded ?
        /// </summary>
        public bool AreLatenciesLoaded { get; set; }
        /// <summary>
        /// Raw list of all sites
        /// </summary>
        public DLL.RawSiteList RawSiteList { get; set; }
        /// <summary>
        /// CCEP files
        /// </summary>
        public List<Latencies> Latencies { get; set; }
        #endregion

        #region Constructors
        public Implantation3D(string name, IEnumerable<string> pts, IEnumerable<string> marsAtlas, IEnumerable<string> patientIDs)
        {
            Name = name;
            PatientElectrodesList = new DLL.PatientElectrodesList();
            IsLoaded = PatientElectrodesList.LoadPTSFiles(pts.ToArray(), marsAtlas.ToArray(), patientIDs.ToArray(), ApplicationState.Module3D.MarsAtlasIndex);
            RawSiteList = new DLL.RawSiteList();
            PatientElectrodesList.ExtractRawSiteList(RawSiteList);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the CCEP for a patient
        /// </summary>
        /// <param name="patient">Patient for these CCEP</param>
        public void LoadLatencies(Data.Patient patient)
        {
            Latencies = new List<Latencies>();
            foreach (Data.Anatomy.Connectivity connectivity in patient.Brain.Connectivities)
            {
                if (!connectivity.WasUsable) continue;
                Latencies latencies = RawSiteList.UpdateLatenciesWithFile(connectivity.File);
                latencies.Name = connectivity.Name;
                Latencies.Add(latencies);
                AreLatenciesLoaded = true;
            }
        }
        #endregion
    }
}