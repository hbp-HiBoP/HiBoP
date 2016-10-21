
/**
 * \file    Electrodes.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define RawPlotList and ElectrodesPatientMultiList classes
 */

// system
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    public class Latencies
    {
        public Latencies(int nbPlots, int[] latencies1D, float[] heights1D)
        {
            stimulationPlots = new bool[nbPlots];
            latencies = new int[nbPlots][];
            heights = new float[nbPlots][];
            transparencies = new float[nbPlots][];
            sizes = new float[nbPlots][];
            positiveHeight = new bool[nbPlots][];
            
            int id = 0;
            for (int ii = 0; ii < nbPlots; ++ii)
            {
                latencies[ii] = new int[nbPlots];
                heights[ii] = new float[nbPlots];
                transparencies[ii] = new float[nbPlots];
                sizes[ii] = new float[nbPlots];
                positiveHeight[ii] = new bool[nbPlots];

                int maxLatency = 0;
                float minHeight = float.MaxValue;
                float maxHeight = float.MinValue;
                for (int jj = 0; jj < nbPlots; ++jj, ++id)
                {
                    latencies[ii][jj] = latencies1D[id];
                    heights[ii][jj] = heights1D[id];

                    if (latencies1D[id] == 0)
                    {
                        stimulationPlots[ii] = true;
                    }
                    else if (latencies1D[id] != -1)
                    {
                        // update max latency for current source plot
                        if (maxLatency < latencies1D[id])
                            maxLatency = latencies1D[id];

                        // update positive height state array
                        positiveHeight[ii][jj] = (heights1D[id] >= 0);

                        // update min/max heights
                        if (heights1D[id] < minHeight)
                            minHeight = heights1D[id];

                        if (heights1D[id] > maxHeight)
                            maxHeight = heights1D[id];
                    }                    
                }

                float max;

                
                if (Math.Abs(minHeight) > Math.Abs(maxHeight))
                {
                    max = Math.Abs(minHeight);
                }
                else
                {
                    max = Math.Abs(maxHeight);
                }

                // now computes transparancies and sizes values 
                for (int jj = 0; jj < nbPlots; ++jj)
                {
                    if (latencies[ii][jj] != 0 && latencies[ii][jj] != -1)
                    {
                        //transparencies[ii][jj] = 1f - (1f* latencies[ii][jj] / maxLatency) * 0.5f;
                        transparencies[ii][jj] = 1f - (0.9f * latencies[ii][jj] / maxLatency);// * 0.5f;
                        sizes[ii][jj] = Math.Abs(heights[ii][jj]) / max;
                    }                    
                }
            }
        }

        public bool isPlotASource(int idPlot)
        {
            return stimulationPlots[idPlot];
        }

        public bool isPlotResponsiveForSource(int idPlotToTest, int idSourcePlot)
        {
            return (latencies[idSourcePlot][idPlotToTest] != -1 && latencies[idSourcePlot][idPlotToTest] != 0);
        }

        public List<int> responsivePlotsId(int idSourcePlot)
        {
            if(!isPlotASource(idSourcePlot))
            {
                Debug.LogError("-ERROR : not a source plot");
                return new List<int>();
            }

            List<int> responsivePlots = new List<int>(latencies.Length);
            for(int ii = 0; ii < latencies.Length; ++ii)
            {
                int latency = latencies[idSourcePlot][ii];
                if (latency != -1 && latency != 0)
                {
                    responsivePlots.Add(ii);
                }
            }
            return responsivePlots;
        }

        public string name;

        bool[] stimulationPlots = null;
        public int[][] latencies = null;
        public float[][] heights = null;

        public float[][] transparencies = null; // for latency
        public float[][] sizes = null;          // for height
        public bool[][] positiveHeight = null;  // for height
    }


    namespace DLL
    {
        /// <summary>
        ///  A raw version of ElectrodesPatientList, means to be used in the C++ DLL
        /// </summary>
        public class RawPlotList : CppDLLImportBase
        {
            #region functions

            /// <summary>
            /// Save the raw plot list to an obj file
            /// </summary>
            /// <param name="pathObjNameFile"></param>
            /// <returns>true if success else false </returns>
            public bool saveToObj(string pathObjNameFile)
            {
                bool success = saveToObj_RawPlotList(_handle, pathObjNameFile);
                DLLDebugManager.checkError();
                return success;
            }

            /// <summary>
            /// Return the number of plots of the list
            /// </summary>
            /// <returns></returns>
            public int getPlotsNumber()
            {
                return getNumberPlot_RawPlotList(_handle);
            }

            /// <summary>
            /// Update the mask of the plot corresponding to the input id
            /// </summary>
            /// <param name="idPlot"></param>
            /// <param name="mask"></param>
            public void updateMask(int idPlot, bool mask)
            {
                updateMask_RawPlotList(_handle, idPlot, mask);
            }

            /// <summary>
            /// ...
            /// </summary>
            /// <param name="latencyFilePath"></param>
            /// <returns></returns>
            public Latencies updateLatenciesWithFile(string latencyFilePath)
            {
                int nbPlots = getPlotsNumber();
                int[] latencies = new int[nbPlots * nbPlots];
                float[] heights = new float[nbPlots * nbPlots];

                bool success = updateLatenciesWithFile_RawPlotList(_handle, latencyFilePath, latencies, heights);
                Debug.Log("-> updateLatenciesWithFile_RawPlotList " + success);
                if (!success)
                    return null;

                Latencies PatientLatencies = new Latencies(nbPlots, latencies, heights);
                return PatientLatencies;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public Latencies generateDummyLatencies()
            {
                int nbPlots = getPlotsNumber();
                int[] latencies = new int[nbPlots * nbPlots];
                float[] heights = new float[nbPlots * nbPlots];

                updateWithDummyLatencies_RawPlotList(_handle, latencies, heights);

                Latencies PatientLatencies = new Latencies(nbPlots, latencies, heights);
                return PatientLatencies;
            }

            /// <summary>
            /// For mani
            /// </summary>
            /// <param name="pathColorFile"></param>
            /// <returns></returns>
            public Color[] loadColors(string pathColorFile)
            {
                int nbPlots = getPlotsNumber();
                float[] r = new float[nbPlots];
                float[] g = new float[nbPlots];
                float[] b = new float[nbPlots];
                bool test = loadColors_RawPlotList(pathColorFile, r, g, b);

                if(!test)
                {
                    Debug.Log("ERRROR");
                    return new Color[nbPlots];
                }

                Color[] colors = new Color[nbPlots];
                for(int ii = 0; ii < nbPlots; ++ii)
                {
                    colors[ii] = new Color(r[ii], g[ii], b[ii]);
                    Debug.Log(ii + " " + colors[ii]);
                }

                return colors;
            }

            #endregion functions

            #region memory_management

            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void createDLLClass()
            {
                _handle = new HandleRef(this, create_RawPlotList());
            }

            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void deleteDLLClass()
            {
                delete_RawPlotList(_handle);
            }

            #endregion memory_management

            #region DLLImport

            //  memory management
            [DllImport("hbp_export", EntryPoint = "create_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_RawPlotList();

            [DllImport("hbp_export", EntryPoint = "delete_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_RawPlotList(HandleRef handleRawPlotLst);

            // actions
            [DllImport("hbp_export", EntryPoint = "updateMask_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void updateMask_RawPlotList(HandleRef handleRawPlotLst, int plotId, bool mask);

            [DllImport("hbp_export", EntryPoint = "updateLatenciesWithFile_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern bool updateLatenciesWithFile_RawPlotList(HandleRef handleRawPlotLst, string pathLatencyFile, int[] latencies, float[] heights);

            [DllImport("hbp_export", EntryPoint = "updateWithDummyLatencies_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void updateWithDummyLatencies_RawPlotList(HandleRef handleRawPlotLst, int[] latencies, float[] heights);

            // save
            [DllImport("hbp_export", EntryPoint = "saveToObj_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern bool saveToObj_RawPlotList(HandleRef handleRawPlotLst, string pathObjNameFile);

            // load
            [DllImport("hbp_export", EntryPoint = "loadColors_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern bool loadColors_RawPlotList(string pathColorFile, float[] r, float[] g, float[] b);

            // retrieve data
            [DllImport("hbp_export", EntryPoint = "getNumberPlot_RawPlotList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getNumberPlot_RawPlotList(HandleRef handleRawPlotLst);

            #endregion DLLImport
        }

        /// <summary>
        ///  A ElectrodesPatientList container, for managing severals patients, more useful for data visualisation, means to be used with unity scripts
        /// </summary>
        public class ElectrodesPatientMultiList : CppDLLImportBase, ICloneable
        {
            #region functions

            /// <summary>
            /// Load a list of pts files add fill ElectrodesPatientMultiList with data
            /// </summary>
            /// <param name="ptsFilesPath"></param>
            /// <returns> true if sucess else false</returns>
            public bool loadNamePtsFiles(List<string> ptsFilesPath, List<string> names)
            {
                string ptsFilesPathStr = "", namesStr = "";
                for (int ii = 0; ii < ptsFilesPath.Count; ++ii)
                {
                    ptsFilesPathStr += ptsFilesPath[ii];

                    if (ii < ptsFilesPath.Count - 1)
                        ptsFilesPathStr += '?';
                }

                for (int ii = 0; ii < names.Count; ++ii)
                {
                    namesStr += names[ii];

                    if (ii < names.Count - 1)
                        namesStr += '?';
                }

                // load in the DLL
                bool fileLoaded = loadPtsFiles_ElectrodesPatientMultiList(_handle, ptsFilesPathStr, namesStr);
                DLLDebugManager.checkError();

                if (!fileLoaded)
                {
                    Debug.LogError("ElectrodesPatientMultiList_dll : Error during loading. " + ptsFilesPathStr);
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Return the total number of plots 
            /// </summary>
            /// <returns></returns>
            public int getTotalPlotsNumber()
            {
                return getNumberPlot_ElectrodesPatientMultiList(_handle);
            }

            /// <summary>
            /// Return the number of patient
            /// </summary>
            /// <returns></returns>
            public int getPatientNumber()
            {
                return getPatientNumber_ElectrodesPatientMultiList(_handle);
            }

            /// <summary>
            /// Return the number of electrode for the input patient id 
            /// </summary>
            /// <param name="patientId"></param>
            /// <returns></returns>
            public int getElectrodeNumber(int patientId)
            {
                return getElectrodeNumber_ElectrodesPatientMultiList(_handle, patientId);
            }

            /// <summary>
            /// Return the eletrode number of plots for the input patient id
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <returns></returns>
            public int getPlotsElectrodeNumber(int patientId, int electrodeId)
            {
                return getPlotsElectrodeNumber_ElectrodesPatientMultiList(_handle, patientId, electrodeId);
            }

            /// <summary>
            /// Return the patient number of plots.
            /// </summary>
            /// <param name="patientId"></param>
            /// <returns></returns>
            public int getPlotsPatientNumber(int patientId)
            {
                return getPlotsPatientNumber_ElectrodesPatientMultiList(_handle, patientId);
            }


            /// <summary>
            /// Set the new state for a patient mask 
            /// </summary>
            /// <param name="state"></param>
            /// <param name="patientId"></param>
            public void setMaskPatient(bool state, int patientId)
            {
                setMaskPatient_ElectrodesPatientMultiList(_handle, state, patientId);
            }

            /// <summary>
            /// Set the new state for an electrode mask 
            /// </summary>
            /// <param name="state"></param>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            public void setMaskElectrode(bool state, int patientId, int electrodeId)
            {
                setMaskElectrode_ElectrodesPatientMultiList(_handle, state, patientId, electrodeId);
            }

            /// <summary>
            /// Set the new state for a plot mask 
            /// </summary>
            /// <param name="state"></param>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="plotId"></param>
            public void setMaskPlot(bool state, int patientId, int electrodeId, int plotId)
            {
                setMaskPlot_ElectrodesPatientMultiList(_handle, state, patientId, electrodeId, plotId);
            }

            /// <summary>
            /// Return the plot mask value
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="plotId"></param>
            /// <returns></returns>
            public bool getPlotMask(int patientId, int electrodeId, int plotId)
            {
                return getPlotMask_ElectrodesPatientMultiList(_handle, patientId, electrodeId, plotId);
            }

            /// <summary>
            /// Return the plot position
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="plotId"></param>
            /// <returns></returns>
            public Vector3 getPlotPos(int patientId, int electrodeId, int plotId)
            {
                float[] pos = new float[3];
                getPlotPos_ElectrodesPatientMultiList(_handle, patientId, electrodeId, plotId, pos);
                return new Vector3(pos[0], pos[1], pos[2]);
            }


            /// <summary>
            /// Return the plot name (electrode name + plot id )
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <param name="plotId"></param>
            /// <returns></returns>
            public string getPlotName(int patientId, int electrodeId, int plotId)
            {
                int length = 6;
                StringBuilder str = new StringBuilder("******");
                getPlotName_ElectrodesPatientMultiList(_handle, patientId, electrodeId, plotId, str, length);

                string name = str.ToString();
                name = name.Replace("?", string.Empty);

                return name;
            }

            /// <summary>
            /// Reset the input raw plot list with ElectrodesPatientMultiList data
            /// </summary>
            /// <param name="rawPlotList"></param>
            public void extractRawPlotList(RawPlotList rawPlotList)
            {
                extractRawPlotList_ElectrodesPatientMultiList(_handle, rawPlotList.getHandle());
            }

            /// <summary>
            /// Update the mask of the input RawPlotList
            /// </summary>
            /// <param name="rawPlotList"></param>
            public void updateRawPlotListMask(RawPlotList rawPlotList)
            {
                updateRawListMask_ElectrodesPatientMultiList(_handle, rawPlotList.getHandle());
            }

            /// <summary>
            /// Return the patient name
            /// </summary>
            /// <param name="patientId"></param>
            /// <returns></returns>
            public string getPatientName(int patientId)
            {
                int length = 30;
                StringBuilder str = new StringBuilder("******************************");
                getPatientName_ElectrodesPatientMultiList(_handle, patientId, str, length);

                string name = str.ToString();
                name = name.Replace("?", string.Empty);

                return name;
            }

            /// <summary>
            /// Return the electrode name
            /// </summary>
            /// <param name="patientId"></param>
            /// <param name="electrodeId"></param>
            /// <returns></returns>
            public string getElectrodeName(int patientId, int electrodeId)
            {
                int length = 30;
                StringBuilder str = new StringBuilder("******************************");
                getElectrodeName_ElectrodesPatientMultiList(_handle, patientId, electrodeId, str, length);

                string name = str.ToString();
                name = name.Replace("?", string.Empty);

                return name;
            }

            /// <summary>
            /// Update all the plots mask an 1D array (used to initialize each column mask electrode list)
            /// </summary>
            /// <param name="maskAllPlots"></param>
            public void updateMask(List<bool> maskAllPlots)
            {
                bool[] mask = maskAllPlots.ToArray();
                updateAllPlotsMask_ElectrodesPatientMultiList(_handle, mask);
            }

            #endregion functions        

            #region memory_management

            /// <summary>
            /// ElectrodesPatientMultiList default constructor
            /// </summary>
            public ElectrodesPatientMultiList() : base() { }

            /// <summary>
            /// ElectrodesPatientMultiList constructor with an already allocated dll ElectrodesPatientMultiList
            /// </summary>
            /// <param name="electrodesPatientMultiListHandle"></param>
            private ElectrodesPatientMultiList(IntPtr electrodesPatientMultiListHandle) : base(electrodesPatientMultiListHandle) { }

            /// <summary>
            /// ElectrodesPatientMultiList_dll copy constructor
            /// </summary>
            /// <param name="other"></param>
            public ElectrodesPatientMultiList(ElectrodesPatientMultiList other) : base(clone_ElectrodesPatientMultiList(other.getHandle())) { }

            /// <summary>
            /// Clone the surface
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new ElectrodesPatientMultiList(this);
            }

            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void createDLLClass()
            {
                _handle = new HandleRef(this, create_ElectrodesPatientMultiList());
            }

            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void deleteDLLClass()
            {
                delete_ElectrodesPatientMultiList(_handle);
            }

            #endregion memory_management

            #region DLLImport

            // memory management
            [DllImport("hbp_export", EntryPoint = "create_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_ElectrodesPatientMultiList();

            [DllImport("hbp_export", EntryPoint = "delete_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_ElectrodesPatientMultiList(HandleRef handlePatientMultiList);

            // memory management
            [DllImport("hbp_export", EntryPoint = "clone_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr clone_ElectrodesPatientMultiList(HandleRef handlePatientMultiListToBeCloned);

            // load
            [DllImport("hbp_export", EntryPoint = "loadPtsFiles_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern bool loadPtsFiles_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, string pathFiles, string names);

            // actions          
            [DllImport("hbp_export", EntryPoint = "extractRawPlotList_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void extractRawPlotList_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, HandleRef handleRawPlotList);

            [DllImport("hbp_export", EntryPoint = "setMaskPatient_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void setMaskPatient_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, bool state, int patientId);

            [DllImport("hbp_export", EntryPoint = "setMaskElectrode_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void setMaskElectrode_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, bool state, int patientId, int electrodeId);

            [DllImport("hbp_export", EntryPoint = "setMaskPlot_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void setMaskPlot_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, bool state, int patientId, int electrodeId, int plotId);

            [DllImport("hbp_export", EntryPoint = "updateRawListMask_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void updateRawListMask_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, HandleRef handleRawPlotList);

            [DllImport("hbp_export", EntryPoint = "updateAllPlotsMask_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void updateAllPlotsMask_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, bool[] maskArray);

            // retrieve data
            [DllImport("hbp_export", EntryPoint = "getPlotName_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getPlotName_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId, int electrodeId, int plotId, StringBuilder name, int length);

            [DllImport("hbp_export", EntryPoint = "getPlotPos_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getPlotPos_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId, int electrodeId, int plotId, float[] position);

            [DllImport("hbp_export", EntryPoint = "getNumberPlot_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getNumberPlot_ElectrodesPatientMultiList(HandleRef handlePatientMultiList);

            [DllImport("hbp_export", EntryPoint = "getPlotMask_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern bool getPlotMask_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId, int electrodeId, int plotId);

            [DllImport("hbp_export", EntryPoint = "getPatientNumber_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getPatientNumber_ElectrodesPatientMultiList(HandleRef handlePatientMultiList);

            [DllImport("hbp_export", EntryPoint = "getPlotsPatientNumber_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getPlotsPatientNumber_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId);

            [DllImport("hbp_export", EntryPoint = "getElectrodeNumber_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getElectrodeNumber_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId);

            [DllImport("hbp_export", EntryPoint = "getPlotsElectrodeNumber_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern int getPlotsElectrodeNumber_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId, int electrodeId);

            [DllImport("hbp_export", EntryPoint = "getPatientName_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getPatientName_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId, StringBuilder name, int length);

            [DllImport("hbp_export", EntryPoint = "getElectrodeName_ElectrodesPatientMultiList", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getElectrodeName_ElectrodesPatientMultiList(HandleRef handlePatientMultiList, int patientId, int electrodeId, StringBuilder name, int length);


            #endregion DLLImport
        }
    }
}