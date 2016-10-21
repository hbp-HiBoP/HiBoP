
/**
 * \file    PatientLoader.cs
 * \author  Lance Florian
 * \date    29/09/2016
 * \brief   Define PatientLoader class
 */

// system
using System;
using System.Text;
using System.Runtime.InteropServices;

using UnityEngine;

namespace HBP.VISU3D.DLL
{

    public struct PatientPaths
    {
        public string IRM, IRMPost;
        public string imp_PRE_SB_PTS, imp_MNI_PTS, imp_POST_SB_PTS;
        public string transfo_T1Pre_to_SB, transfo_T1Post_to_SB, transfo_T1Post_to_T1Pre;
        public string head, Lhemi, Rhemi, Lwhite, Rwhite;
    }

    /// <summary>
    /// 
    /// </summary>
    public class PatientLoader : CppDLLImportBase
    {



        #region functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathPatientsDir"></param>
        /// <returns></returns>
        //public int load(string pathPatientsDir)
        //{
        //    return load_PatientLoader(_handle, pathPatientsDir);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string name(int index)
        {
            int length = 30;
            StringBuilder str = new StringBuilder();
            for(int ii = 0; ii < length; ++ii)
                str.Insert(0, '*');

            patientName_PatientLoader(_handle, index, str, length);

            string nameStr = str.ToString();
            nameStr = nameStr.Replace("?", string.Empty);

            return nameStr;            
        }

        public bool displayPostIRM() { return displayPostIRM_PatientLoader(_handle); }

        public int checkEvents()
        {
            return checkEvents_PatientLoader(_handle);
        }

        public int nbPatients()
        {
            return nbPatients_PatientLoader(_handle);
        }

        public bool isPatientValid(int index)
        {
            return isValided_PatientLoader(_handle, index);
        }

        public int currentPatientIndex()
        {
            return nbPatients_currentPatientIndex(_handle);
        }

        public PatientPaths patientPaths(int index)
        {
            PatientPaths paths;

            int length = 300;
            StringBuilder IRMStr = new StringBuilder(), IRMPostStr = new StringBuilder();
            StringBuilder imp_PRE_SB_PTSStr = new StringBuilder(), imp_MNI_PTSStr = new StringBuilder(), imp_POST_SB_PTSStr = new StringBuilder();
            StringBuilder transfo_T1Pre_to_SBStr = new StringBuilder(), transfo_T1Post_to_SBStr = new StringBuilder(), transfo_T1Post_to_T1PreStr = new StringBuilder();
            StringBuilder headStr = new StringBuilder(), LhemiStr = new StringBuilder(), RhemiStr = new StringBuilder(), LwhiteStr = new StringBuilder(), RwhiteStr = new StringBuilder();

            IRMStr.Insert(0, "*", length); IRMPostStr.Insert(0, "*", length);
            imp_PRE_SB_PTSStr.Insert(0, "*", length); imp_MNI_PTSStr.Insert(0, "*", length); imp_POST_SB_PTSStr.Insert(0, "*", length);
            transfo_T1Pre_to_SBStr.Insert(0, "*", length); transfo_T1Post_to_SBStr.Insert(0, "*", length); transfo_T1Post_to_T1PreStr.Insert(0, "*", length);
            headStr.Insert(0, "*", length); LhemiStr.Insert(0, "*", length); RhemiStr.Insert(0, "*", length); LwhiteStr.Insert(0, "*", length); RwhiteStr.Insert(0, "*", length);

            patientPaths_PatientLoader(_handle, index, length,
                IRMStr, IRMPostStr,
                imp_PRE_SB_PTSStr, imp_MNI_PTSStr, imp_POST_SB_PTSStr,
                transfo_T1Pre_to_SBStr, transfo_T1Post_to_SBStr, transfo_T1Post_to_T1PreStr,
                headStr, LhemiStr, RhemiStr, LwhiteStr, RwhiteStr);

            IRMStr.Replace("?", ""); IRMPostStr.Replace("?", "");
            imp_PRE_SB_PTSStr.Replace("?", ""); imp_MNI_PTSStr.Replace("?", ""); imp_POST_SB_PTSStr.Replace("?", "");
            transfo_T1Pre_to_SBStr.Replace("?", ""); transfo_T1Post_to_SBStr.Replace("?", ""); transfo_T1Post_to_T1PreStr.Replace("?", "");
            headStr.Replace("?", ""); LhemiStr.Replace("?", ""); RhemiStr.Replace("?", ""); LwhiteStr.Replace("?", ""); RwhiteStr.Replace("?", "");

            paths.IRM = IRMStr.ToString(); paths.IRMPost = IRMPostStr.ToString();
            paths.imp_PRE_SB_PTS = imp_PRE_SB_PTSStr.ToString(); paths.imp_POST_SB_PTS = imp_POST_SB_PTSStr.ToString(); paths.imp_MNI_PTS = imp_MNI_PTSStr.ToString();
            paths.transfo_T1Pre_to_SB = transfo_T1Pre_to_SBStr.ToString(); paths.transfo_T1Post_to_T1Pre = transfo_T1Post_to_T1PreStr.ToString(); paths.transfo_T1Post_to_SB = transfo_T1Post_to_SBStr.ToString();
            paths.head = headStr.ToString(); paths.Lhemi = LhemiStr.ToString(); paths.Rhemi = RhemiStr.ToString(); paths.Lwhite = LwhiteStr.ToString(); paths.Rwhite = RwhiteStr.ToString();

            return paths;
        }


        #endregion functions

        #region memory_management

        /// <summary>
        /// PatientLoader default constructor
        /// </summary>
        public PatientLoader()
        {}

        /// <summary>
        /// PatientLoader constructor with an already allocated dll BBox
        /// </summary>
        /// <param name="bBoxPointer"></param>
        public PatientLoader(IntPtr patientLoaderPointer)
        {
            _handle = new HandleRef(this, patientLoaderPointer);
        }

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void createDLLClass()
        {
            _handle = new HandleRef(this, create_PatientLoader());
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void deleteDLLClass()
        {
            delete_PatientLoader(_handle);
        }

        #endregion memory_management

        #region DLLImport

        // memory management
        [DllImport("hbp_export", EntryPoint = "create_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_PatientLoader();

        [DllImport("hbp_export", EntryPoint = "delete_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_PatientLoader(HandleRef loaderUI);

        // I/O
        //[DllImport("hbp_export", EntryPoint = "load_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        //static private extern int load_PatientLoader(HandleRef loaderUI, string pathPatientsDir);

        // data
        [DllImport("hbp_export", EntryPoint = "patientName_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern void patientName_PatientLoader(HandleRef loaderUI, int index, StringBuilder name, int length);

        [DllImport("hbp_export", EntryPoint = "isValided_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool isValided_PatientLoader(HandleRef loaderUI, int index);

        [DllImport("hbp_export", EntryPoint = "displayPostIRM_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool displayPostIRM_PatientLoader(HandleRef loaderUI);

        [DllImport("hbp_export", EntryPoint = "patientPaths_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern void patientPaths_PatientLoader(  HandleRef loaderUI, int index, int length,
                                                                StringBuilder IRM, StringBuilder IRMPost,
                                                                StringBuilder imp_PRE_SB_PTS, StringBuilder imp_MNI_PTS, StringBuilder imp_POST_SB_PTS,
                                                                StringBuilder transfo_T1Pre_to_SB, StringBuilder transfo_T1Post_to_SB, StringBuilder transfo_T1Post_to_T1Pre,
                                                                StringBuilder head, StringBuilder Lhemi, StringBuilder Rhemi, StringBuilder Lwhite, StringBuilder Rwhite);

        [DllImport("hbp_export", EntryPoint = "nbPatients_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern int nbPatients_PatientLoader(HandleRef loaderUI);

        [DllImport("hbp_export", EntryPoint = "nbPatients_currentPatientIndex", CallingConvention = CallingConvention.Cdecl)]
        static private extern int nbPatients_currentPatientIndex(HandleRef loaderUI);        

        // events
        [DllImport("hbp_export", EntryPoint = "checkEvents_PatientLoader", CallingConvention = CallingConvention.Cdecl)]
        static private extern int checkEvents_PatientLoader(HandleRef loaderUI);
        

        #endregion DLLImport
    }
}