using System.IO;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Tools;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Global class containing information about the MNI meshes and MRIs
    /// </summary>
    public class MNIObjects
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
        /// MRI of the MNI
        /// </summary>
        public MRI3D MRI { get; private set; }

        /// <summary>
        /// Are the MNI objects completely loaded ?
        /// </summary>
        public bool IsLoaded { get; private set; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load the MNI objects
        /// </summary>
        /// <param name="mniMRIDir">Directory of the MRI of the MNI</param>
        /// <param name="mniMeshDir">Directory of the meshes of the MNI</param>
        private async UniTask LoadDataAsync(string mniMRIDir, string mniMeshDir)
        {
            await UniTask.SwitchToThreadPool();
            DLL.Volume volume = new();
            volume.LoadNIFTIFile(Path.Combine(mniMRIDir, "MNI.nii"));
            MRI = new MRI3D("MNI", volume);

            DLL.Surface leftHemi = new();
            DLL.Surface rightHemi = new();
            DLL.Surface bothHemi;
            leftHemi.LoadGIIFile(Path.Combine(mniMeshDir, "MNI_Lhemi.gii"), Path.Combine(mniMeshDir, "MNI.trm"));
            leftHemi.FlipTriangles();
            rightHemi.LoadGIIFile(Path.Combine(mniMeshDir, "MNI_Rhemi.gii"), Path.Combine(mniMeshDir, "MNI.trm"));
            rightHemi.FlipTriangles();
            bothHemi = (DLL.Surface)leftHemi.Clone();
            bothHemi.Append(rightHemi);
            leftHemi.ComputeNormals();
            rightHemi.ComputeNormals();
            bothHemi.ComputeNormals();
            GreyMatter = new LeftRightMesh3D("MNI Grey matter", leftHemi, rightHemi, bothHemi, MeshType.MNI);

            DLL.Surface leftWhite = new();
            DLL.Surface rightWhite = new();
            DLL.Surface bothWhite;
            leftWhite.LoadGIIFile(Path.Combine(mniMeshDir, "MNI_Lwhite.gii"), Path.Combine(mniMeshDir, "MNI.trm"));
            leftWhite.FlipTriangles();
            rightWhite.LoadGIIFile(Path.Combine(mniMeshDir, "MNI_Rwhite.gii"), Path.Combine(mniMeshDir, "MNI.trm"));
            rightWhite.FlipTriangles();
            bothWhite = (DLL.Surface)leftWhite.Clone();
            bothWhite.Append(rightWhite);
            leftWhite.ComputeNormals();
            rightWhite.ComputeNormals();
            bothWhite.ComputeNormals();
            WhiteMatter = new LeftRightMesh3D("MNI White matter", leftWhite, rightWhite, bothWhite, MeshType.MNI);
        }

        #endregion

        #region Public Methods

        public async UniTaskVoid Load()
        {
            string baseIRMDir = Path.Combine(ApplicationState.DataPath, "IRM"), baseMeshDir = Path.Combine(ApplicationState.DataPath, "Meshes");
            await LoadDataAsync(baseIRMDir, baseMeshDir);
            IsLoaded = true;
        }

        public void Clean()
        {
            GreyMatter?.Clean();
            WhiteMatter?.Clean();
            MRI?.Clean();
        }

        #endregion
    }
}
