
/**
 * \file    Electrodes.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Latencies, MarsAtlasIndex, RawSiteList and PatientElectrodesList classes
 */

// system
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// CCEP latencies
    /// </summary>
    public class Latencies
    {
        #region Properties
        public string Name;

        bool[] m_StimulationPlots = null;
        public int[][] LatenciesValues = null;
        public float[][] Heights = null;

        public float[][] Transparencies = null; // for latency
        public float[][] Sizes = null;          // for height
        public bool[][] PositiveHeight = null;  // for height
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbPlots"></param>
        /// <param name="latencies1D"></param>
        /// <param name="heights1D"></param>
        public Latencies(int nbPlots, int[] latencies1D, float[] heights1D)
        {
            m_StimulationPlots = new bool[nbPlots];
            LatenciesValues = new int[nbPlots][];
            Heights = new float[nbPlots][];
            Transparencies = new float[nbPlots][];
            Sizes = new float[nbPlots][];
            PositiveHeight = new bool[nbPlots][];
            
            int id = 0;
            for (int ii = 0; ii < nbPlots; ++ii)
            {
                LatenciesValues[ii] = new int[nbPlots];
                Heights[ii] = new float[nbPlots];
                Transparencies[ii] = new float[nbPlots];
                Sizes[ii] = new float[nbPlots];
                PositiveHeight[ii] = new bool[nbPlots];

                int maxLatency = 0;
                float minHeight = float.MaxValue;
                float maxHeight = float.MinValue;
                for (int jj = 0; jj < nbPlots; ++jj, ++id)
                {
                    LatenciesValues[ii][jj] = latencies1D[id];
                    Heights[ii][jj] = heights1D[id];

                    if (latencies1D[id] == 0)
                    {
                        m_StimulationPlots[ii] = true;
                    }
                    else if (latencies1D[id] != -1)
                    {
                        // update max latency for current source plot
                        if (maxLatency < latencies1D[id])
                            maxLatency = latencies1D[id];

                        // update positive height state array
                        PositiveHeight[ii][jj] = (heights1D[id] >= 0);

                        // update min/max heights
                        if (heights1D[id] < minHeight)
                            minHeight = heights1D[id];

                        if (heights1D[id] > maxHeight)
                            maxHeight = heights1D[id];
                    }                    
                }

                float max;

                
                if (Math.Abs(minHeight) > Math.Abs(maxHeight)) {
                    max = Math.Abs(minHeight);
                }
                else {
                    max = Math.Abs(maxHeight);
                }

                // now computes transparencies and sizes values 
                for (int jj = 0; jj < nbPlots; ++jj)
                {
                    if (LatenciesValues[ii][jj] != 0 && LatenciesValues[ii][jj] != -1) {
                        Transparencies[ii][jj] = 1f - (0.9f * LatenciesValues[ii][jj] / maxLatency);
                        Sizes[ii][jj] = Math.Abs(Heights[ii][jj]) / max;
                    }                    
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idSite"></param>
        /// <returns></returns>
        public bool IsSiteASource(int idSite)
        {
            return m_StimulationPlots[idSite];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idSiteToTest"></param>
        /// <param name="idSourceSite"></param>
        /// <returns></returns>
        public bool IsSiteResponsiveForSource(int idSiteToTest, int idSourceSite)
        {
            return (LatenciesValues[idSourceSite][idSiteToTest] != -1 && LatenciesValues[idSourceSite][idSiteToTest] != 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idSourceSite"></param>
        /// <returns></returns>
        public List<int> ResponsiveSiteID(int idSourceSite)
        {
            if(!IsSiteASource(idSourceSite))
            {
                Debug.LogError("-ERROR : not a source site.");
                return new List<int>();
            }

            List<int> responsiveSites = new List<int>(LatenciesValues.Length);
            for(int ii = 0; ii < LatenciesValues.Length; ++ii)
            {
                int latency = LatenciesValues[idSourceSite][ii];
                if (latency != -1 && latency != 0)
                {
                    responsiveSites.Add(ii);
                }
            }
            return responsiveSites;
        }
        #endregion
    }


    namespace DLL
    {
        /// <summary>
        /// Mars atlas index, used to identify sites mars IDs
        /// </summary>
        public class MarsAtlasIndex : CppDLLImportBase
        {
            #region Constructors
            public MarsAtlasIndex(string path) : base()
            {
                if (!LoadMarsAtlasIndexFile(path))
                {
                    Debug.LogError("Can't load mars atlas index.");
                }
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// 
            /// </summary>
            /// <param name="pathFile"></param>
            /// <returns></returns>
            public bool LoadMarsAtlasIndexFile(string pathFile)
            {
                return load_MarsAtlasIndex(_handle, pathFile) == 1;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="label"></param>
            /// <returns></returns>
            public string Hemisphere(int label)
            {
                int length = 3;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                hemisphere_MarsAtlasIndex(_handle, label, str, length);
                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="label"></param>
            /// <returns></returns>
            public string Lobe(int label)
            {
                int length = 15;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                lobe_MarsAtlasIndex(_handle, label, str, length);
                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="label"></param>
            /// <returns></returns>
            public string NameFS(int label)
            {
                int length = 30;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                nameFS_MarsAtlasIndex(_handle, label, str, length);
                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="label"></param>
            /// <returns></returns>
            public string Name(int label)
            {
                int length = 10;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                name_MarsAtlasIndex(_handle, label, str, length);
                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="label"></param>
            /// <returns></returns>
            public string FullName(int label)
            {
                if (label < 0) return "not found";

                int length = 50;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                fullName_MarsAtlasIndex(_handle, label, str, length);

                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="label"></param>
            /// <returns></returns>
            public string BroadmanArea(int label)
            {
                if (label < 0) return "not found";

                int length = 100;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                BA_MarsAtlasIndex(_handle, label, str, length);
                return str.ToString().Replace("?", string.Empty);
            }
            #endregion

            #region Memory Management
            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this, create_MarsAtlasIndex());
            }
            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void delete_DLL_class()
            {
                delete_MarsAtlasIndex(_handle);
            }
            #endregion

            #region DLLImport

            [DllImport("hbp_export", EntryPoint = "create_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_MarsAtlasIndex();
            [DllImport("hbp_export", EntryPoint = "delete_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_MarsAtlasIndex(HandleRef marsAtlasIndex);
            // load
            [DllImport("hbp_export", EntryPoint = "load_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern int load_MarsAtlasIndex(HandleRef marsAtlasIndex, string pathFile);
            // retrieve data
            [DllImport("hbp_export", EntryPoint = "hemisphere_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern void hemisphere_MarsAtlasIndex(HandleRef marsAtlasIndex, int label, StringBuilder hemisphere, int length);
            [DllImport("hbp_export", EntryPoint = "lobe_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern void lobe_MarsAtlasIndex(HandleRef marsAtlasIndex, int label, StringBuilder lobe, int length);
            [DllImport("hbp_export", EntryPoint = "nameFS_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern void nameFS_MarsAtlasIndex(HandleRef marsAtlasIndex, int label, StringBuilder nameFS, int length);
            [DllImport("hbp_export", EntryPoint = "name_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern void name_MarsAtlasIndex(HandleRef marsAtlasIndex, int label, StringBuilder name, int length);
            [DllImport("hbp_export", EntryPoint = "fullName_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern void fullName_MarsAtlasIndex(HandleRef marsAtlasIndex, int label, StringBuilder fullName, int length);
            [DllImport("hbp_export", EntryPoint = "BA_MarsAtlasIndex", CallingConvention = CallingConvention.Cdecl)]
            static private extern void BA_MarsAtlasIndex(HandleRef marsAtlasIndex, int label, StringBuilder BA, int length);

            #endregion

        }

        /// <summary>
        ///  A raw version of PatientElectrodesList, means to be used in the C++ DLL
        /// </summary>
        public class RawSiteList : CppDLLImportBase
        {
            #region Properties
            /// <summary>
            /// Number of sites
            /// </summary>
            public int NumberOfSites
            {
                get
                {
                    return sites_nb_RawSiteList(_handle);
                }
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// Save the raw plot list to an obj file
            /// </summary>
            /// <param name="pathObjNameFile"></param>
            /// <returns>true if success else false </returns>
            public bool SaveToObj(string pathObjNameFile)
            {
                bool success = saveToObj_RawPlotList(_handle, pathObjNameFile) == 1;
                ApplicationState.DLLDebugManager.check_error();
                return success;
            }
            /// <summary>
            /// Update the mask of the site corresponding to the input id
            /// </summary>
            /// <param name="idSite"></param>
            /// <param name="mask"></param>
            public void UpdateMask(int idSite, bool mask)
            {
                update_mask_RawSiteList(_handle, idSite, mask ? 1 : 0);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="latencyFilePath"></param>
            /// <returns></returns>
            public Latencies UpdateLatenciesWithFile(string latencyFilePath)
            {
                int nbPlots = NumberOfSites;
                int[] latencies = new int[nbPlots * nbPlots];
                float[] heights = new float[nbPlots * nbPlots];

                bool success = update_latencies_with_file_RawSiteList(_handle, latencyFilePath, latencies, heights) == 1;                
                if (!success)
                    return null;

                Latencies PatientLatencies = new Latencies(nbPlots, latencies, heights);
                return PatientLatencies;
            }
            /// <summary>
            /// Generate dummy latencies for debug purposes
            /// </summary>
            /// <returns></returns>
            public Latencies GenerateDummyLatencies()
            {
                int nbPlots = NumberOfSites;
                int[] latencies = new int[nbPlots * nbPlots];
                float[] heights = new float[nbPlots * nbPlots];

                update_with_dummy_latencies_RawSiteList(_handle, latencies, heights);

                Latencies PatientLatencies = new Latencies(nbPlots, latencies, heights);
                return PatientLatencies;
            }
            /// <summary>
            /// Get an array containing bool values telling if a site is on a plane or not considering a specific precision
            /// </summary>
            /// <param name="plane"></param>
            /// <param name="precision"></param>
            /// <param name="result"></param>
            public void GetSitesOnPlane(Plane plane, float precision, out int[] result)
            {
                result = new int[NumberOfSites];
                float[] planeV = new float[6];
                for (int ii = 0; ii < 3; ++ii)
                {
                    planeV[ii] = plane.Point[ii];
                    planeV[ii + 3] = plane.Normal[ii];
                }
                sites_on_plane_RawSiteList(_handle, planeV, precision, result);
            }
            /// <summary>
            /// Returns true if a site is on a place, false otherwise
            /// </summary>
            /// <param name="site"></param>
            /// <param name="plane"></param>
            /// <param name="precision"></param>
            /// <returns></returns>
            public bool IsSiteOnAnyPlane(Site site, IEnumerable<Plane> planes, float precision)
            {
                bool result = false;
                foreach (var plane in planes)
                {
                    float[] planeV = new float[6];
                    for (int ii = 0; ii < 3; ++ii)
                    {
                        planeV[ii] = plane.Point[ii];
                        planeV[ii + 3] = plane.Normal[ii];
                    }
                    result |= is_site_on_plane_RawSiteList(_handle, site.Information.GlobalID, planeV, precision) == 1;
                }
                return result;
            }
            #endregion

            #region Memory Management
            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this, create_RawSiteList());
            }
            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void delete_DLL_class()
            {
                delete_RawSiteList(_handle);
            }
            #endregion

            #region DLLImport

            //  memory management
            [DllImport("hbp_export", EntryPoint = "create_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_RawSiteList();

            [DllImport("hbp_export", EntryPoint = "delete_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_RawSiteList(HandleRef handleRawSiteLst);

            // actions
            [DllImport("hbp_export", EntryPoint = "update_mask_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_mask_RawSiteList(HandleRef handleRawSiteLst, int plotId, int mask);

            [DllImport("hbp_export", EntryPoint = "update_latencies_with_file_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int update_latencies_with_file_RawSiteList(HandleRef handleRawSiteLst, string pathLatencyFile, int[] latencies, float[] heights);

            [DllImport("hbp_export", EntryPoint = "update_with_dummy_latencies_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_with_dummy_latencies_RawSiteList(HandleRef handleRawSiteLst, int[] latencies, float[] heights);

            // save
            [DllImport("hbp_export", EntryPoint = "saveToObj_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int saveToObj_RawPlotList(HandleRef handleRawSiteLst, string pathObjNameFile);

            // retrieve data
            [DllImport("hbp_export", EntryPoint = "sites_nb_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int sites_nb_RawSiteList(HandleRef handleRawSiteLst);
            
            [DllImport("hbp_export", EntryPoint = "is_site_on_plane_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int is_site_on_plane_RawSiteList(HandleRef handleRawSiteLst, int siteID, float[] planeV, float precision);

            [DllImport("hbp_export", EntryPoint = "sites_on_plane_RawSiteList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void sites_on_plane_RawSiteList(HandleRef handleRawSiteLst, float[] planeV, float precision, int[] result);

            //  memory management
            //delegate IntPtr create_RawPlotList();
            //delegate void delete_RawPlotList(HandleRef handleRawPlotLst);
            //// actions
            //delegate void updateMask_RawPlotList(HandleRef handleRawPlotLst, int plotId, int mask);
            //delegate int updateLatenciesWithFile_RawPlotList(HandleRef handleRawPlotLst, string pathLatencyFile, int[] latencies, float[] heights);
            //delegate void updateWithDummyLatencies_RawPlotList(HandleRef handleRawPlotLst, int[] latencies, float[] heights);
            //// save
            //delegate int saveToObj_RawPlotList(HandleRef handleRawPlotLst, string pathObjNameFile);
            //// load
            //delegate int loadColors_RawPlotList(string pathColorFile, float[] r, float[] g, float[] b);
            //// retrieve data
            //delegate int getNumberPlot_RawPlotList(HandleRef handleRawPlotLst);

            #endregion
        }

        /// <summary>
        ///  A PatientElectrodes container, for managing severals patients, more useful for data visualization, means to be used with unity scripts
        /// </summary>
        public class PatientElectrodesList : CppDLLImportBase, ICloneable
        {
            #region Properties
            /// <summary>
            /// Number of sites
            /// </summary>
            public int TotalSitesNumber
            {
                get
                {
                    return sites_nb_PatientElectrodesList(_handle);
                }
            }
            /// <summary>
            /// Number of patients
            /// </summary>
            public int NumberOfPatients
            {
                get
                {
                    return patients_nb_PatientElectrodesList(_handle);
                }
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// Load a list of pts files add fill ElectrodesPatientMultiList with data
            /// </summary>
            /// <param name="ptsFilesPath"></param>
            /// <returns> true if sucess else false</returns>
            public bool LoadPTSFiles(string[] ptsFilesPath, string[] marsAtlas, string[] names, MarsAtlasIndex marsAtlasIndex)
            {
                string ptsFilesPathStr = string.Join("?", ptsFilesPath);
                string marsAtlasStr = string.Join("?", marsAtlas);
                string namesStr = string.Join("?", names);

                // load in the DLL
                bool fileLoaded = load_Pts_files_PatientElectrodesList(_handle, ptsFilesPathStr, marsAtlasStr, namesStr, marsAtlasIndex.getHandle()) == 1;
                ApplicationState.DLLDebugManager.check_error();

                if (!fileLoaded)
                {
                    Debug.LogError("PatientElectrodesList : Error during loading. " + ptsFilesPathStr);
                    return false;
                }

                return true;
            }
            /// <summary>
            /// Return the number of electrode for the input patient id 
            /// </summary>
            /// <param name="patientId"></param>
            /// <returns></returns>
            public int NumberOfElectrodesInPatient(int patientId)
            {
                return electrodes_nb_PatientElectrodesList(_handle, patientId);
            }
            /// <summary>
            /// Return the eletrode number of plots for the input patient id
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <returns></returns>
            public int NumberOfSitesInElectrode(int patientId, int electrodeId)
            { 
                return electrode_sites_nb_PatientElectrodesList(_handle, patientId, electrodeId);
            }
            /// <summary>
            /// Return the patient number of sites.
            /// </summary>
            /// <param name="patientId"></param>
            /// <returns></returns>
            public int NumberOfSitesInPatient(int patientId)
            {
                return patient_sites_nb_PatientElectrodesList(_handle, patientId);
            }
            /// <summary>
            /// Set the new state for a patient mask 
            /// </summary>
            /// <param name="state"></param>
            /// <param name="patientId"></param>
            public void SetPatientMask(bool state, int patientId)
            {
                set_mask_patient_PatientElectrodesList(_handle, state ? 1 : 0, patientId);
            }
            /// <summary>
            /// Set the new state for an electrode mask 
            /// </summary>
            /// <param name="state"></param>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            public void SetElectrodeMask(bool state, int patientId, int electrodeId)
            {
                set_mask_electrode_PatientElectrodesList(_handle, state ? 1 : 0, patientId, electrodeId);
            }
            /// <summary>
            /// Set the new state for a site mask 
            /// </summary>
            /// <param name="state"></param>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="siteId"></param>
            public void SetSiteMask(bool state, int patientId, int electrodeId, int siteId)
            {                
                set_mask_site_PatientElectrodesList(_handle, state ? 1 : 0, patientId, electrodeId, siteId);
            }
            /// <summary>
            /// Return the site mask value
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="siteId"></param>
            /// <returns></returns>
            public bool SiteMask(int patientId, int electrodeId, int siteId)
            {
                return site_mask_PatientElectrodesList(_handle, patientId, electrodeId, siteId) == 1;
            }
            /// <summary>
            /// Return the site position
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="siteId"></param>
            /// <returns></returns>
            public Vector3 SitePosition(int patientId, int electrodeId, int siteId)
            {
                float[] pos = new float[3];                
                site_pos_PatientElectrodesList(_handle, patientId, electrodeId, siteId, pos);
                return new Vector3(pos[0], pos[1], pos[2]);
            }
            /// <summary>
            /// Return the site name (electrode name + plot id )
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="siteId"></param>
            /// <returns></returns>
            public string SiteName(int patientId, int electrodeId, int siteId)
            {
                int length = 8;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                site_name_PatientElectrodesList(_handle, patientId, electrodeId, siteId, str, length);

                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// Reset the input raw site list with PatientElectrodesList data
            /// </summary>
            /// <param name="rawPlotList"></param>
            public void ExtractRawSiteList(RawSiteList rawSiteList)
            {
                extract_raw_site_list_PatientElectrodesList(_handle, rawSiteList.getHandle());
            }
            /// <summary>
            /// Update the mask of the input rawSiteList
            /// </summary>
            /// <param name="rawSiteList"></param>
            public void UpdateRawSiteListMask(RawSiteList rawSiteList)
            {
                update_raw_site_list_mask_PatientElectrodesList(_handle, rawSiteList.getHandle());
            }
            /// <summary>
            /// Return the patient name
            /// </summary>
            /// <param name="patientId"></param>
            /// <returns></returns>
            public string PatientName(int patientId)
            {
                int length = 30;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                patient_name_PatientElectrodesList(_handle, patientId, str, length);

                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// Return the electrode name
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <returns></returns>
            public string ElectrodeName(int patientId, int electrodeId)
            {
                int length = 30;
                StringBuilder str = new StringBuilder();
                str.Append('?', length);
                electrode_name_PatientElectrodesList(_handle, patientId, electrodeId, str, length);

                return str.ToString().Replace("?", string.Empty);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="siteId"></param>
            /// <returns></returns>
            public int MarsAtlasLabelOfSite(int patientId, int electrodeId, int siteId)
            {
                return site_mars_atlas_label_PatientElectrodesList(_handle, patientId, electrodeId, siteId);
            }
            #endregion

            #region Memory Management
            /// <summary>
            /// ElectrodesPatientMultiList default constructor
            /// </summary>
            public PatientElectrodesList() : base() { }
            /// <summary>
            /// ElectrodesPatientMultiList constructor with an already allocated dll ElectrodesPatientMultiList
            /// </summary>
            /// <param name="electrodesPatientMultiListHandle"></param>
            private PatientElectrodesList(IntPtr electrodesPatientMultiListHandle) : base(electrodesPatientMultiListHandle) { }
            /// <summary>
            /// ElectrodesPatientMultiList_dll copy constructor
            /// </summary>
            /// <param name="other"></param>
            public PatientElectrodesList(PatientElectrodesList other) : base(clone_PatientElectrodesList(other.getHandle())) { }
            /// <summary>
            /// Clone the surface
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new PatientElectrodesList(this);
            }
            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this, create_PatientElectrodesList());
            }
            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void delete_DLL_class()
            {
                delete_PatientElectrodesList(_handle);
            }
            #endregion

            #region DLLImport

            // memory management
            [DllImport("hbp_export", EntryPoint = "create_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_PatientElectrodesList();

            [DllImport("hbp_export", EntryPoint = "delete_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_PatientElectrodesList(HandleRef handlePatientElectrodesList);

            // memory management
            [DllImport("hbp_export", EntryPoint = "clone_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr clone_PatientElectrodesList(HandleRef handlePatientElectrodesListToBeCloned);

            // load
            [DllImport("hbp_export", EntryPoint = "load_Pts_files_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int load_Pts_files_PatientElectrodesList(HandleRef handlePatientElectrodesList, string pathFiles, string marsAtlas, string names, HandleRef handleMarsAtlasIndex);

            // actions          
            [DllImport("hbp_export", EntryPoint = "extract_raw_site_list_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void extract_raw_site_list_PatientElectrodesList(HandleRef handlePatientElectrodesList, HandleRef handleRawSiteList);

            [DllImport("hbp_export", EntryPoint = "set_mask_patient_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void set_mask_patient_PatientElectrodesList(HandleRef handlePatientElectrodesList, int state, int patientId);

            [DllImport("hbp_export", EntryPoint = "set_mask_electrode_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void set_mask_electrode_PatientElectrodesList(HandleRef handlePatientElectrodesList, int state, int patientId, int electrodeId);

            [DllImport("hbp_export", EntryPoint = "set_mask_site_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void set_mask_site_PatientElectrodesList(HandleRef handlePatientElectrodesList, int state, int patientId, int electrodeId, int siteId);

            [DllImport("hbp_export", EntryPoint = "update_raw_site_list_mask_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_raw_site_list_mask_PatientElectrodesList(HandleRef handlePatientElectrodesList, HandleRef handleRawSiteList);

            [DllImport("hbp_export", EntryPoint = "update_all_sites_mask_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_all_sites_mask_PatientElectrodesList(HandleRef handlePatientElectrodesList, int[] maskArray);

            // retrieve data
            [DllImport("hbp_export", EntryPoint = "site_name_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int site_name_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId, int electrodeId, int siteId, StringBuilder name, int length);

            [DllImport("hbp_export", EntryPoint = "site_pos_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void site_pos_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId, int electrodeId, int siteId, float[] position);

            [DllImport("hbp_export", EntryPoint = "sites_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int sites_nb_PatientElectrodesList(HandleRef handlePatientElectrodesList);

            [DllImport("hbp_export", EntryPoint = "site_mask_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int site_mask_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId, int electrodeId, int siteId);

            [DllImport("hbp_export", EntryPoint = "patients_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int patients_nb_PatientElectrodesList(HandleRef handlePatientElectrodesList);

            [DllImport("hbp_export", EntryPoint = "patient_sites_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int patient_sites_nb_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId);

            [DllImport("hbp_export", EntryPoint = "electrodes_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int electrodes_nb_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId);

            [DllImport("hbp_export", EntryPoint = "electrode_sites_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int electrode_sites_nb_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId, int electrodeId);

            [DllImport("hbp_export", EntryPoint = "patient_name_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void patient_name_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId, StringBuilder name, int length);

            [DllImport("hbp_export", EntryPoint = "electrode_name_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void electrode_name_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId, int electrodeId, StringBuilder name, int length);

            [DllImport("hbp_export", EntryPoint = "site_mars_atlas_label_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int site_mars_atlas_label_PatientElectrodesList(HandleRef handlePatientElectrodesList, int patientId, int electrodeId, int siteId);

            #endregion
        }
    }
}