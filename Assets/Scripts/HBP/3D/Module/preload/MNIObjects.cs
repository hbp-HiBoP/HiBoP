
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

#if UNITY_EDITOR_WIN
        private DLL.ReadMultiFilesBuffers readMulti = null;
#endif
        #endregion

        #region Private Methods
        void Awake()
        {
            int idScript = TimeExecution.ID;
            TimeExecution.StartAwake(idScript, TimeExecution.ScriptsId.MNIObjects);

            string dataDirPath = GlobalPaths.Data;
        
            int instanceID = GetInstanceID();
            string nameGO = name;

            string baseIRMDir = dataDirPath + "IRM/", baseMeshDir = dataDirPath + "Meshes/";
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
            LoadData(baseIRMDir, baseMeshDir, idScript, nameGO, instanceID);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseIRMDir"></param>
        /// <param name="baseMeshDir"></param>
        /// <param name="idScript"></param>
        /// <param name="GOName"></param>
        /// <param name="instanceID"></param>
        void LoadData(string baseIRMDir, string baseMeshDir, int idScript, string GOName, int instanceID)
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
            LeftHemi.load_GII_file(baseMeshDir + "MNI_single_hight_Lhemi.gii", true, baseMeshDir + "transfo_mni.trm"); LeftHemi.flip_triangles();
            RightHemi.load_GII_file(baseMeshDir + "MNI_single_hight_Rhemi.gii", true, baseMeshDir + "transfo_mni.trm"); RightHemi.flip_triangles();
            BothHemi = (DLL.Surface)LeftHemi.Clone();
            BothHemi.add(RightHemi);
            LeftHemi.compute_normals();
            RightHemi.compute_normals();
            BothHemi.compute_normals();

            LeftWhite = new DLL.Surface();
            RightWhite = new DLL.Surface();
            BothWhite = new DLL.Surface();
            LeftWhite.load_GII_file(baseMeshDir + "MNI_single_hight_Lwhite.gii", true, baseMeshDir + "transfo_mni.trm"); LeftWhite.flip_triangles();
            RightWhite.load_GII_file(baseMeshDir + "MNI_single_hight_Rwhite.gii", true, baseMeshDir + "transfo_mni.trm"); RightWhite.flip_triangles();
            BothWhite = (DLL.Surface)LeftWhite.Clone();
            BothWhite.add(RightWhite);
            LeftWhite.compute_normals();
            RightWhite.compute_normals();
            BothWhite.compute_normals();

            LeftWhiteInflated = new DLL.Surface();
            RightWhiteInflated = new DLL.Surface();
            BothWhiteInflated = new DLL.Surface();
            LeftWhiteInflated.load_GII_file(baseMeshDir + "MNI_single_hight_Lwhite_inflated.gii", true, baseMeshDir + "transfo_mni.trm"); LeftWhiteInflated.flip_triangles();
            RightWhiteInflated.load_GII_file(baseMeshDir + "MNI_single_hight_Rwhite_inflated.gii", true, baseMeshDir + "transfo_mni.trm"); RightWhiteInflated.flip_triangles();
            BothWhiteInflated = (DLL.Surface)LeftWhiteInflated.Clone();
            BothWhiteInflated.add(RightWhiteInflated);
            LeftWhiteInflated.compute_normals();
            RightWhiteInflated.compute_normals();
            BothWhiteInflated.compute_normals();

#endif
            LoadingMutex.ReleaseMutex();
            TimeExecution.EndAwake(idScript, TimeExecution.ScriptsId.MNIObjects, GOName, instanceID);
        }
        #endregion
    }
}