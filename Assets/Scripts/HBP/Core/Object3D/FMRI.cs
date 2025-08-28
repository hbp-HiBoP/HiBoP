using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace HBP.Core.Object3D
{
    public class FMRI
    {
        #region Properties
        /// <summary>
        /// Name of the MRI
        /// </summary>
        public string Name { get; set; }
        private string m_File = "";
        public DLL.NIFTI NIFTI { get; private set; } = new DLL.NIFTI();
        public List<DLL.Volume> Volumes { get; private set; } = new List<DLL.Volume>();
        public bool Loading { get; private set; } = false;
        public bool Loaded { get; private set; } = false;
        #endregion

        #region Constructors
        public FMRI()
        {
            Name = "Default";
            Volumes.Add(new DLL.Volume());
        }
        public FMRI(Data.MRI mri, bool loadInBackground = true) : this(mri.Name, mri.File, loadInBackground)
        {
        }
        public FMRI(string name, string file, bool loadInBackground = true)
        {
            Name = name;
            m_File = file;
            if (loadInBackground)
                Load(file).Forget();
        }
        #endregion

        #region Private Methods
        private async UniTaskVoid Load(string file)
        {
            await LoadAsync(file);
        }
        /// <summary>
        /// Load the FMRI
        /// </summary>
        private async UniTask LoadAsync(string file)
        {
            await UniTask.SwitchToThreadPool();
            Loading = true;
            NIFTI.Load(file);
            for (int i = 0; i < NIFTI.NumberOfVolumes; i++)
            {
                Volumes.Add(NIFTI.ExtractVolume(i));
            }
            Loading = false;
            Loaded = true;
        }
        #endregion

        #region Public Methods
        public async UniTask LoadAsync()
        {
            await LoadAsync(m_File);
        }
        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public void Clean()
        {
            foreach (var volume in Volumes)
            {
                volume.Dispose();
            }
            NIFTI.Dispose();
        }
        #endregion
    }
}