using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D
{
    public class Implantation3D
    {
        #region Properties
        public string Name { get; set; }
        public DLL.RawSiteList RawSiteList { get; set; }
        public DLL.PatientElectrodesList PatientElectrodesList { get; set; }
        public bool IsLoaded { get; set; }
        #endregion

        #region Constructors
        public Implantation3D(string name, IEnumerable<string> pts, IEnumerable<string> patientIDs)
        {
            Name = name;
            PatientElectrodesList = new DLL.PatientElectrodesList();
            RawSiteList = new DLL.RawSiteList();
            IsLoaded = PatientElectrodesList.LoadPTSFiles(pts.ToList(), patientIDs.ToList(), ApplicationState.Module3D.MarsAtlasIndex);
            PatientElectrodesList.ExtractRawSiteList(RawSiteList);

            // FIXME : Something has been commented. See one of the first commits for more information
            // reset latencies
            //m_ColumnManager.LatenciesFiles = new List<Latencies>();
            //CCEPLabels = new List<string>();
            //for (int ii = 0; ii < Patient.Brain.Connectivities.Count; ++ii)
            //{
            //    Latencies latencies = null;
            //    if (Patient.Brain.SitesConnectivities == "dummyPath" || Patient.Brain.SitesConnectivities == string.Empty)
            //    {
            //        // generate dummy latencies
            //        latencies = m_ColumnManager.DLLLoadedRawSitesList.GenerateDummyLatencies();
            //    }
            //    else
            //    {
            //        // load latency file
            //        latencies = m_ColumnManager.DLLLoadedRawSitesList.UpdateLatenciesWithFile(Patient.Brain.SitesConnectivities);// Connectivities[ii].Path);
            //    }

            //    if (latencies != null)
            //    {
            //        latencies.Name = Patient.Brain.SitesConnectivities; //Connectivities[ii].Label;
            //        m_ColumnManager.LatenciesFiles.Add(latencies);
            //        CCEPLabels.Add(latencies.Name);
            //    }

            //    //latencies = m_CM.DLLLoadedRawPlotsList.updateLatenciesWithFile("C:/Users/Florian/Desktop/amplitudes_latencies/amplitudes_latencies/SIEJO_amplitudes_latencies.txt");

            //}

            //m_ColumnManager.LatencyFilesDefined = false; //(Patient.Brain.Connectivities.Count > 0);
            //OnUpdateLatencies.Invoke(CCEPLabels);
        }
        #endregion
    }
}