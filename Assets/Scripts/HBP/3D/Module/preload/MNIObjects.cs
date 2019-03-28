
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

        public LeftRightMesh3D GreyMatter { get; private set; }
        public LeftRightMesh3D WhiteMatter { get; private set; }
        public LeftRightMesh3D InflatedWhiteMatter { get; private set; }
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
        private void OnDestroy()
        {
            GreyMatter.Clean();
            WhiteMatter.Clean();
            InflatedWhiteMatter.Clean();
            MRI.Clean();
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

            DLL.NIFTI nii = new DLL.NIFTI();
            nii.LoadNIIFile(baseIRMDir + "MNI.nii");
            DLL.Volume volume = new DLL.Volume();
            nii.ConvertToVolume(volume);
            MRI = new MRI3D("MNI", nii, volume);

            DLL.Surface leftHemi = new DLL.Surface();
            DLL.Surface rightHemi = new DLL.Surface();
            DLL.Surface bothHemi;
            leftHemi.LoadGIIFile(baseMeshDir + "MNI_Lhemi.gii", true, baseMeshDir + "MNI.trm"); leftHemi.FlipTriangles();
            rightHemi.LoadGIIFile(baseMeshDir + "MNI_Rhemi.gii", true, baseMeshDir + "MNI.trm"); rightHemi.FlipTriangles();
            bothHemi = (DLL.Surface)leftHemi.Clone();
            bothHemi.Append(rightHemi);
            leftHemi.ComputeNormals();
            rightHemi.ComputeNormals();
            bothHemi.ComputeNormals();
            GreyMatter = new LeftRightMesh3D("MNI Grey matter", leftHemi, rightHemi, bothHemi);

            DLL.Surface leftWhite = new DLL.Surface();
            DLL.Surface rightWhite = new DLL.Surface();
            DLL.Surface bothWhite;
            leftWhite.LoadGIIFile(baseMeshDir + "MNI_Lwhite.gii", true, baseMeshDir + "MNI.trm"); leftWhite.FlipTriangles();
            rightWhite.LoadGIIFile(baseMeshDir + "MNI_Rwhite.gii", true, baseMeshDir + "MNI.trm"); rightWhite.FlipTriangles();
            bothWhite = (DLL.Surface)leftWhite.Clone();
            bothWhite.Append(rightWhite);
            leftWhite.ComputeNormals();
            rightWhite.ComputeNormals();
            bothWhite.ComputeNormals();
            WhiteMatter = new LeftRightMesh3D("MNI White matter", leftWhite, rightWhite, bothWhite);

            DLL.Surface leftWhiteInflated = new DLL.Surface();
            DLL.Surface rightWhiteInflated = new DLL.Surface();
            DLL.Surface bothWhiteInflated;
            leftWhiteInflated.LoadGIIFile(baseMeshDir + "MNI_Lwhite_inflated.gii", true, baseMeshDir + "MNI.trm"); leftWhiteInflated.FlipTriangles();
            rightWhiteInflated.LoadGIIFile(baseMeshDir + "MNI_Rwhite_inflated.gii", true, baseMeshDir + "MNI.trm"); rightWhiteInflated.FlipTriangles();
            bothWhiteInflated = (DLL.Surface)leftWhiteInflated.Clone();
            bothWhiteInflated.Append(rightWhiteInflated);
            leftWhiteInflated.ComputeNormals();
            rightWhiteInflated.ComputeNormals();
            bothWhiteInflated.ComputeNormals();
            InflatedWhiteMatter = new LeftRightMesh3D("MNI Inflated", leftWhiteInflated, rightWhiteInflated, bothWhiteInflated);

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