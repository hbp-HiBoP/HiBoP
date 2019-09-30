
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
            GreyMatter?.Clean();
            WhiteMatter?.Clean();
            InflatedWhiteMatter?.Clean();
            MRI?.Clean();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mniMRIDir"></param>
        /// <param name="mniMeshDir"></param>
        /// <param name="idScript"></param>
        /// <param name="GOName"></param>
        /// <param name="instanceID"></param>
        void LoadData(string mniMRIDir, string mniMeshDir, string GOName, int instanceID)
        {
            DLL.Volume volume = new DLL.Volume();
            volume.LoadNIFTIFile(mniMRIDir + "MNI.nii");
            MRI = new MRI3D("MNI", volume);

            DLL.Surface leftHemi = new DLL.Surface();
            DLL.Surface rightHemi = new DLL.Surface();
            DLL.Surface bothHemi;
            leftHemi.LoadGIIFile(mniMeshDir + "MNI_Lhemi.gii", mniMeshDir + "MNI.trm"); leftHemi.FlipTriangles();
            rightHemi.LoadGIIFile(mniMeshDir + "MNI_Rhemi.gii", mniMeshDir + "MNI.trm"); rightHemi.FlipTriangles();
            bothHemi = (DLL.Surface)leftHemi.Clone();
            bothHemi.Append(rightHemi);
            leftHemi.ComputeNormals();
            rightHemi.ComputeNormals();
            bothHemi.ComputeNormals();
            GreyMatter = new LeftRightMesh3D("MNI Grey matter", leftHemi, rightHemi, bothHemi, Data.Enums.MeshType.MNI);

            DLL.Surface leftWhite = new DLL.Surface();
            DLL.Surface rightWhite = new DLL.Surface();
            DLL.Surface bothWhite;
            leftWhite.LoadGIIFile(mniMeshDir + "MNI_Lwhite.gii", mniMeshDir + "MNI.trm"); leftWhite.FlipTriangles();
            rightWhite.LoadGIIFile(mniMeshDir + "MNI_Rwhite.gii", mniMeshDir + "MNI.trm"); rightWhite.FlipTriangles();
            bothWhite = (DLL.Surface)leftWhite.Clone();
            bothWhite.Append(rightWhite);
            leftWhite.ComputeNormals();
            rightWhite.ComputeNormals();
            bothWhite.ComputeNormals();
            WhiteMatter = new LeftRightMesh3D("MNI White matter", leftWhite, rightWhite, bothWhite, Data.Enums.MeshType.MNI);

            DLL.Surface leftWhiteInflated = new DLL.Surface();
            DLL.Surface rightWhiteInflated = new DLL.Surface();
            DLL.Surface bothWhiteInflated;
            leftWhiteInflated.LoadGIIFile(mniMeshDir + "MNI_Lwhite_inflated.gii", mniMeshDir + "MNI.trm"); leftWhiteInflated.FlipTriangles();
            rightWhiteInflated.LoadGIIFile(mniMeshDir + "MNI_Rwhite_inflated.gii", mniMeshDir + "MNI.trm"); rightWhiteInflated.FlipTriangles();
            bothWhiteInflated = (DLL.Surface)leftWhiteInflated.Clone();
            bothWhiteInflated.Append(rightWhiteInflated);
            leftWhiteInflated.ComputeNormals();
            rightWhiteInflated.ComputeNormals();
            bothWhiteInflated.ComputeNormals();
            InflatedWhiteMatter = new LeftRightMesh3D("MNI Inflated", leftWhiteInflated, rightWhiteInflated, bothWhiteInflated, Data.Enums.MeshType.MNI);
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