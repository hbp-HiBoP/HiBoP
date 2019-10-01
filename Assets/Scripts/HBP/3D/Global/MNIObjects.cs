using System.Collections;
using UnityEngine;
using CielaSpike;

namespace HBP.Module3D
{
    /// <summary>
    /// Global class containing information about the MNI meshes and MRIs
    /// </summary>
    public class MNIObjects : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Mesh of the Grey Matter
        /// </summary>
        public LeftRightMesh3D GreyMatter { get; private set; }
        /// <summary>
        /// Mesh of the White Matter
        /// </summary>
        public LeftRightMesh3D WhiteMatter { get; private set; }
        /// <summary>
        /// Mesh of the Inflated White Matter
        /// </summary>
        public LeftRightMesh3D InflatedWhiteMatter { get; private set; }
        /// <summary>
        /// MRI of the MNI
        /// </summary>
        public MRI3D MRI { get; private set; }

        /// <summary>
        /// Path to the files to load
        /// </summary>
        private string m_DataPath = "";
        /// <summary>
        /// Are the MNI objects completely loaded ?
        /// </summary>
        public bool IsLoaded { get; private set; }
        #endregion

        #region Private Methods
        private void Awake()
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
        /// Load the MNI objects
        /// </summary>
        /// <param name="mniMRIDir">Directory of the MRI of the MNI</param>
        /// <param name="mniMeshDir">Directory of the meshes of the MNI</param>
        private void LoadData(string mniMRIDir, string mniMeshDir)
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
        /// <summary>
        /// Coroutine to load the MNI objects
        /// </summary>
        /// <returns>Coroutine return</returns>
        public IEnumerator c_Load()
        {
            yield return Ninja.JumpBack;
            string baseIRMDir = m_DataPath + "IRM/", baseMeshDir = m_DataPath + "Meshes/";
            LoadData(baseIRMDir, baseMeshDir);
            yield return Ninja.JumpToUnity;
            IsLoaded = true;
        }
        #endregion
    }
}