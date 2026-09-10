using System.IO;
using System.Linq;
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

        private AsyncLazy m_LoadWork;

        public System.Collections.Generic.Dictionary<string, string> ResourceHashes { get; private set; }

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
            var owned = new System.Collections.Generic.List<DLL.Surface>();

            DLL.Surface Own(DLL.Surface surface)
            {
                owned.Add(surface);
                return surface;
            }

            var volume = new DLL.Volume();
            try
            {
                if (!volume.LoadNIFTIFile(Path.Combine(mniMRIDir, "MNI.nii"))) throw new IOException("MNI MRI could not be loaded.");

                LeftRightMesh3D Prepare(string name, string leftFile, string rightFile)
                {
                    var left = Own(new DLL.Surface());
                    var right = Own(new DLL.Surface());
                    string transformation = Path.Combine(mniMeshDir, "MNI.trm");
                    if (!left.LoadGIIFile(Path.Combine(mniMeshDir, leftFile), transformation) || !right.LoadGIIFile(Path.Combine(mniMeshDir, rightFile), transformation)) throw new IOException("MNI surface could not be loaded.");
                    left.FlipTriangles();
                    right.FlipTriangles();
                    var both = Own((DLL.Surface)left.Clone());
                    both.Append(right);
                    left.ComputeNormals();
                    right.ComputeNormals();
                    both.ComputeNormals();
                    var simplifiedLeft = Own(left.Simplify());
                    var simplifiedRight = Own(right.Simplify());
                    var simplifiedBoth = Own(both.Simplify());
                    return new LeftRightMesh3D(name, MeshType.MNI, both, simplifiedBoth, left, right, simplifiedLeft, simplifiedRight, shared: true);
                }

                var grey = Prepare("MNI Grey matter", "MNI_Lhemi.gii", "MNI_Rhemi.gii");
                var white = Prepare("MNI White matter", "MNI_Lwhite.gii", "MNI_Rwhite.gii");
                MRI = new MRI3D("MNI", volume);
                GreyMatter = grey;
                WhiteMatter = white;
                owned.Clear();
            }
            catch
            {
                foreach (var surface in owned) surface.Dispose();
                volume.Dispose();
                throw;
            }
        }

        #endregion

        #region Public Methods

        public UniTask Load()
        {
            m_LoadWork ??= UniTask.Lazy(LoadCoreAsync);
            return m_LoadWork.Task;
        }

        private async UniTask LoadCoreAsync()
        {
            await StandardData.EnsureInstalledAsync();
            await UniTask.SwitchToThreadPool();
            var hashes = StandardData.EnumerateFiles(ApplicationState.DataPath).ToDictionary(path => path, path => StandardData.HashFile(StandardData.Resolve(ApplicationState.DataPath, path)));
            string baseIRMDir = Path.Combine(ApplicationState.DataPath, "IRM"), baseMeshDir = Path.Combine(ApplicationState.DataPath, "Meshes");
            try
            {
                await LoadDataAsync(baseIRMDir, baseMeshDir);
                if (!GreyMatter.IsLoaded || !WhiteMatter.IsLoaded || !MRI.IsLoaded) throw new System.IO.IOException("MNI reference data could not be loaded.");
                foreach (var entry in hashes)
                    if (StandardData.HashFile(StandardData.Resolve(ApplicationState.DataPath, entry.Key)) != entry.Value)
                        throw new System.IO.IOException("Reference data changed during loading.");
                ResourceHashes = hashes;
                IsLoaded = true;
            }
            catch
            {
                Clean();
                throw;
            }
        }

        public void Clean()
        {
            GreyMatter?.Clean();
            WhiteMatter?.Clean();
            MRI?.Clean();
            GreyMatter = WhiteMatter = null;
            MRI = null;
            ResourceHashes = null;
            IsLoaded = false;
            m_LoadWork = null;
        }

        #endregion
    }
}
