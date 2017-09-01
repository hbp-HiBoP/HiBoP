
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

        public DLL.Surface LeftHemi = null;
        public DLL.Surface RightHemi = null;
        public DLL.Surface BothHemi = null;

        public DLL.Surface LeftWhite = null;
        public DLL.Surface RightWhite = null;
        public DLL.Surface BothWhite = null;

        public DLL.Surface LeftWhiteInflated = null;
        public DLL.Surface RightWhiteInflated = null;
        public DLL.Surface BothWhiteInflated = null;

        // ch256.nii
        public DLL.Volume IRM = null;

        public DLL.NIFTI NII = null;

        private string m_DataPath = "";

#if UNITY_EDITOR_WIN
        private DLL.ReadMultiFilesBuffers readMulti = null;
#endif
        #endregion

        #region Private Methods
        void Awake()
        {
            m_DataPath = GlobalPaths.Data;
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
            
            IRM = new DLL.Volume();
            NII.ConvertToVolume(IRM);

#if UNITY_EDITOR_WIN

            readMulti.ParseMeshes();

            List<DLL.Surface> meshes = readMulti.Meshes();
            for (int ii = 0; ii < meshes.Count; ++ii)
                meshes[ii].ComputeNormals();

            LeftHemi = meshes[0];
            RightHemi = meshes[1];
            BothHemi = meshes[2];

            LeftWhite = meshes[3];
            RightWhite = meshes[4];
            BothWhite = meshes[5];

            LeftWhiteInflated = meshes[6];
            RightWhiteInflated = meshes[7];
            BothWhiteInflated = meshes[8];

#else

            LeftHemi = new DLL.Surface();
            RightHemi = new DLL.Surface();
            BothHemi = new DLL.Surface();
            LeftHemi.LoadGIIFile(baseMeshDir + "MNI_single_hight_Lhemi.gii", true, baseMeshDir + "transfo_mni.trm"); LeftHemi.FlipTriangles();
            RightHemi.LoadGIIFile(baseMeshDir + "MNI_single_hight_Rhemi.gii", true, baseMeshDir + "transfo_mni.trm"); RightHemi.FlipTriangles();
            BothHemi = (DLL.Surface)LeftHemi.Clone();
            BothHemi.Add(RightHemi);
            LeftHemi.ComputeNormals();
            RightHemi.ComputeNormals();
            BothHemi.ComputeNormals();

            LeftWhite = new DLL.Surface();
            RightWhite = new DLL.Surface();
            BothWhite = new DLL.Surface();
            LeftWhite.LoadGIIFile(baseMeshDir + "MNI_single_hight_Lwhite.gii", true, baseMeshDir + "transfo_mni.trm"); LeftWhite.FlipTriangles();
            RightWhite.LoadGIIFile(baseMeshDir + "MNI_single_hight_Rwhite.gii", true, baseMeshDir + "transfo_mni.trm"); RightWhite.FlipTriangles();
            BothWhite = (DLL.Surface)LeftWhite.Clone();
            BothWhite.Add(RightWhite);
            LeftWhite.ComputeNormals();
            RightWhite.ComputeNormals();
            BothWhite.ComputeNormals();

            LeftWhiteInflated = new DLL.Surface();
            RightWhiteInflated = new DLL.Surface();
            BothWhiteInflated = new DLL.Surface();
            LeftWhiteInflated.LoadGIIFile(baseMeshDir + "MNI_single_hight_Lwhite_inflated.gii", true, baseMeshDir + "transfo_mni.trm"); LeftWhiteInflated.FlipTriangles();
            RightWhiteInflated.LoadGIIFile(baseMeshDir + "MNI_single_hight_Rwhite_inflated.gii", true, baseMeshDir + "transfo_mni.trm"); RightWhiteInflated.FlipTriangles();
            BothWhiteInflated = (DLL.Surface)LeftWhiteInflated.Clone();
            BothWhiteInflated.Add(RightWhiteInflated);
            LeftWhiteInflated.ComputeNormals();
            RightWhiteInflated.ComputeNormals();
            BothWhiteInflated.ComputeNormals();

#endif
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
            // IRM
            NII = new DLL.NIFTI();
            NII.LoadNIIFile(baseIRMDir + "ch256.nii");

            List<string> filesPaths = new List<string>(9);
            filesPaths.Add(baseMeshDir + "MNI_single_hight_Lhemi.obj");
            filesPaths.Add(baseMeshDir + "MNI_single_hight_Rhemi.obj");
            filesPaths.Add(baseMeshDir + "MNI_single_hight_Bhemi.obj");

            filesPaths.Add(baseMeshDir + "MNI_single_hight_Lwhite.obj");
            filesPaths.Add(baseMeshDir + "MNI_single_hight_Rwhite.obj");
            filesPaths.Add(baseMeshDir + "MNI_single_hight_Bwhite.obj");

            filesPaths.Add(baseMeshDir + "MNI_single_hight_Lwhite_inflated.obj");
            filesPaths.Add(baseMeshDir + "MNI_single_hight_Rwhite_inflated.obj");
            filesPaths.Add(baseMeshDir + "MNI_single_hight_Bwhite_inflated.obj");

#if UNITY_EDITOR_WIN
            readMulti = new DLL.ReadMultiFilesBuffers();
            readMulti.ReadBuffersFiles(filesPaths, DLL.ReadMultiFilesBuffers.FilesTypes.MeshesObj);
#endif
            //Thread thread = new Thread(() => LoadData(baseIRMDir, baseMeshDir, idScript, nameGO, instanceID));
            //thread.Start();
            LoadData(baseIRMDir, baseMeshDir, nameGO, instanceID);
        }
        #endregion
    }
}