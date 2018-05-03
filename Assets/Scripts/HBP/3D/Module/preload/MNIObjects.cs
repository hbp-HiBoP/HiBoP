
/* \file MNIObjects.cs
 * \author Lance Florian
 * \date    22/04/2016
 * \brief Define MNIObjects
 */

// system
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// unity
using UnityEngine;
using UnityEngine.Events;
using CielaSpike;

namespace HBP.Module3D
{
    /// <summary>
    /// MNI meshes and MRI preloaded at start
    /// </summary>
    public class MNIObjects : MonoBehaviour
    {
        #region Properties
        public static Mutex LoadingMutex = new Mutex();

        private DLL.Surface m_LeftHemi = null;
        private DLL.Surface m_RightHemi = null;
        private DLL.Surface m_BothHemi = null;
        public LeftRightMesh3D GreyMatter { get; private set; }

        private DLL.Surface m_LeftWhite = null;
        private DLL.Surface m_RightWhite = null;
        private DLL.Surface m_BothWhite = null;
        public LeftRightMesh3D WhiteMatter { get; private set; }

        private DLL.Surface m_LeftWhiteInflated = null;
        private DLL.Surface m_RightWhiteInflated = null;
        private DLL.Surface m_BothWhiteInflated = null;
        public LeftRightMesh3D InflatedWhiteMatter { get; private set; }

        private DLL.Volume m_Volume = null;
        private DLL.NIFTI m_NII = null;
        public MRI3D MRI { get; private set; }

        private string m_DataPath = "";

        public bool Loaded { get; private set; }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_DataPath = ApplicationState.DataPath;
            this.StartCoroutineAsync(c_Load());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseIRMDir"></param>
        /// <param name="baseMeshDir"></param>
        /// <param name="idScript"></param>
        /// <param name="GOName"></param>
        /// <param name="instanceID"></param>
        void LoadData(string baseIRMDir, string baseMeshDir, string GOName, int instanceID)
        {
            LoadingMutex.WaitOne();

            m_NII = new DLL.NIFTI();
            m_NII.LoadNIIFile(baseIRMDir + "MNI.nii");
            m_Volume = new DLL.Volume();
            m_NII.ConvertToVolume(m_Volume);
            MRI = new MRI3D("MNI", m_NII, m_Volume);

            m_LeftHemi = new DLL.Surface();
            m_RightHemi = new DLL.Surface();
            m_BothHemi = new DLL.Surface();
            m_LeftHemi.LoadGIIFile(baseMeshDir + "MNI_single_hight_Lhemi.gii", true, baseMeshDir + "transfo_mni.trm"); m_LeftHemi.FlipTriangles();
            m_RightHemi.LoadGIIFile(baseMeshDir + "MNI_single_hight_Rhemi.gii", true, baseMeshDir + "transfo_mni.trm"); m_RightHemi.FlipTriangles();
            m_BothHemi = (DLL.Surface)m_LeftHemi.Clone();
            m_BothHemi.Append(m_RightHemi);
            m_LeftHemi.ComputeNormals();
            m_RightHemi.ComputeNormals();
            m_BothHemi.ComputeNormals();
            GreyMatter = new LeftRightMesh3D("MNI Grey matter", m_LeftHemi, m_RightHemi, m_BothHemi);

            m_LeftWhite = new DLL.Surface();
            m_RightWhite = new DLL.Surface();
            m_BothWhite = new DLL.Surface();
            m_LeftWhite.LoadGIIFile(baseMeshDir + "MNI_single_hight_Lwhite.gii", true, baseMeshDir + "transfo_mni.trm"); m_LeftWhite.FlipTriangles();
            m_RightWhite.LoadGIIFile(baseMeshDir + "MNI_single_hight_Rwhite.gii", true, baseMeshDir + "transfo_mni.trm"); m_RightWhite.FlipTriangles();
            m_BothWhite = (DLL.Surface)m_LeftWhite.Clone();
            m_BothWhite.Append(m_RightWhite);
            m_LeftWhite.ComputeNormals();
            m_RightWhite.ComputeNormals();
            m_BothWhite.ComputeNormals();
            WhiteMatter = new LeftRightMesh3D("MNI White matter", m_LeftWhite, m_RightWhite, m_BothWhite);

            m_LeftWhiteInflated = new DLL.Surface();
            m_RightWhiteInflated = new DLL.Surface();
            m_BothWhiteInflated = new DLL.Surface();
            m_LeftWhiteInflated.LoadGIIFile(baseMeshDir + "MNI_single_hight_Lwhite_inflated.gii", true, baseMeshDir + "transfo_mni.trm"); m_LeftWhiteInflated.FlipTriangles();
            m_RightWhiteInflated.LoadGIIFile(baseMeshDir + "MNI_single_hight_Rwhite_inflated.gii", true, baseMeshDir + "transfo_mni.trm"); m_RightWhiteInflated.FlipTriangles();
            m_BothWhiteInflated = (DLL.Surface)m_LeftWhiteInflated.Clone();
            m_BothWhiteInflated.Append(m_RightWhiteInflated);
            m_LeftWhiteInflated.ComputeNormals();
            m_RightWhiteInflated.ComputeNormals();
            m_BothWhiteInflated.ComputeNormals();
            InflatedWhiteMatter = new LeftRightMesh3D("MNI Inflated", m_LeftWhiteInflated, m_RightWhiteInflated, m_BothWhiteInflated);

            LoadingMutex.ReleaseMutex();
        }
        #endregion

        #region Coroutines
        public IEnumerator c_Load()
        {
            yield return Ninja.JumpToUnity;
            int instanceID = GetInstanceID();
            string nameGO = name;
            yield return Ninja.JumpBack;
            string baseIRMDir = m_DataPath + "IRM/", baseMeshDir = m_DataPath + "Meshes/";
            LoadData(baseIRMDir, baseMeshDir, nameGO, instanceID);
            yield return Ninja.JumpToUnity;
            Loaded = true;
        }
        #endregion
    }
}