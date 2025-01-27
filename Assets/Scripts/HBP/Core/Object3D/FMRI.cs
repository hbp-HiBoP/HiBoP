using System.Collections.Generic;
using System.Threading.Tasks;

namespace HBP.Core.Object3D
{
    public class FMRI
    {
        #region Properties
        /// <summary>
        /// Name of the MRI
        /// </summary>
        public string Name { get; set; }
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
        public FMRI(Data.MRI mri) : this(mri.Name, mri.File)
        {
        }
        public FMRI(string name, string file)
        {
            Name = name;
            Load(file);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Load the FMRI
        /// </summary>
        private async void Load(string file)
        {
            await Task.Run(() =>
            {
                Loading = true;
                NIFTI.Load(file);
                for (int i = 0; i < NIFTI.NumberOfVolumes; i++)
                {
                    Volumes.Add(NIFTI.ExtractVolume(i));
                }
                Loading = false;
                Loaded = true;
            });
        }
        #endregion

        #region Public Methods
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