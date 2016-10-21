
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


namespace HBP.VISU3D
{
    public class MNIObjects : MonoBehaviour
    {
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

        private DLL.NIFTI NII = new DLL.NIFTI();
        private DLL.ReadMultiFilesBuffers readMulti = new DLL.ReadMultiFilesBuffers();

        void Awake()
        {

            int idScript = TimeExecution.getId();
            TimeExecution.startAwake(idScript, TimeExecution.ScriptsId.MNIObjects);
            
            string baseDir = Application.dataPath + "/../Data/";
        #if UNITY_EDITOR
                    baseDir = Application.dataPath + "/Data/";
        #endif

            int instanceID = GetInstanceID();
            string nameGO = name;

            string baseIRMDir = baseDir + "IRM/", baseMeshDir = baseDir + "Meshes/";
            // IRM
            NII = new DLL.NIFTI();
            NII.loadNiftiFile(baseIRMDir + "ch256.nii");

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

            //if(RuntimePlatform.LinuxPlayer) §§§ TODO

            readMulti.readBuffersFiles(filesPaths, DLL.ReadMultiFilesBuffers.FilesTypes.MeshesObj);


            //loadData(baseIRMDir, baseMeshDir, idScript, nameGO, instanceID);

            Thread thread = new Thread(() => loadData(baseIRMDir, baseMeshDir, idScript, nameGO, instanceID));
            thread.Start();
        }


        void loadData(string baseIRMDir, string baseMeshDir, int idScript, string GOName, int instanceID)
        {
            LoadingMutex.WaitOne();

            //double t = TimeExecution.getWorldTime();
            
            IRM = new DLL.Volume();
            NII.convertToVolume(IRM);


            //Debug.LogError((TimeExecution.getWorldTime() - t).ToString("0.00"));

            readMulti.parseMeshes();

            List<DLL.Surface> meshes = readMulti.getMeshes();
            for (int ii = 0; ii < meshes.Count; ++ii)
                meshes[ii].computeNormals();

            LeftHemi = meshes[0];
            RightHemi = meshes[1];
            BothHemi = meshes[2];

            LeftWhite = meshes[3];
            RightWhite = meshes[4];
            BothWhite = meshes[5];

            LeftWhiteInflated = meshes[6];
            RightWhiteInflated = meshes[7];
            BothWhiteInflated = meshes[8];


            //Debug.LogError((TimeExecution.getWorldTime() - t).ToString("0.00"));

            //// meshes
            ////  hemi
            //LeftHemi = new DLL.Surface();
            //LeftHemi.loadObjFile(baseMeshDir + "MNI_single_hight_Lhemi.obj");

            //Debug.LogError((TimeExecution.getWorldTime() - t).ToString("0.00"));
            //LeftHemi.computeNormals();

            //Debug.LogError((TimeExecution.getWorldTime() - t).ToString("0.00"));

            //RightHemi = new DLL.Surface();
            //RightHemi.loadObjFile(baseMeshDir + "MNI_single_hight_Rhemi.obj");
            //RightHemi.computeNormals();

            //Debug.LogError((TimeExecution.getWorldTime() - t).ToString("0.00"));

            //BothHemi = new DLL.Surface();
            //BothHemi.loadObjFile(baseMeshDir + "MNI_single_hight_Bhemi.obj");
            //BothHemi.computeNormals();

            //Debug.LogError((TimeExecution.getWorldTime() - t).ToString("0.00"));
            ////  white
            //LeftWhite = new DLL.Surface();
            //LeftWhite.loadObjFile(baseMeshDir + "MNI_single_hight_Lwhite.obj");
            //LeftWhite.computeNormals();

            //RightWhite = new DLL.Surface();
            //RightWhite.loadObjFile(baseMeshDir + "MNI_single_hight_Rwhite.obj");
            //RightWhite.computeNormals();

            //BothWhite = new DLL.Surface();
            //BothWhite.loadObjFile(baseMeshDir + "MNI_single_hight_Bwhite.obj");
            //BothWhite.computeNormals();

            ////  inflated
            //LeftWhiteInflated = new DLL.Surface();
            //LeftWhiteInflated.loadObjFile(baseMeshDir + "MNI_single_hight_Lwhite_inflated.obj");
            //LeftWhiteInflated.computeNormals();

            //RightWhiteInflated = new DLL.Surface();
            //RightWhiteInflated.loadObjFile(baseMeshDir + "MNI_single_hight_Rwhite_inflated.obj");
            //RightWhiteInflated.computeNormals();

            //BothWhiteInflated = new DLL.Surface();
            //BothWhiteInflated.loadObjFile(baseMeshDir + "MNI_single_hight_Bwhite_inflated.obj");
            //BothWhiteInflated.computeNormals();

            LoadingMutex.ReleaseMutex();

            TimeExecution.endAwake(idScript, TimeExecution.ScriptsId.MNIObjects, GOName, instanceID);
        }
    }
}