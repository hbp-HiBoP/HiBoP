using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using System.Collections.Generic;
using System.Diagnostics;

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
        private string m_MaskFile = "";
        public List<Volume> Volumes { get; private set; } = new List<Volume>();
        public Volume MaskVolume { get; private set; } = new Volume();
        public bool Loading { get; private set; } = false;
        public bool Loaded { get; private set; } = false;

        // Store these properties so we can dispose the NIFTI object after loading
        public float StartTime { get; private set; } = 1;
        public float TimeStep { get; private set; } = 1;
        public string TimeUnit { get; private set; } = "dt";
        public MRICalValues ExtremeValues { get; private set; }
        public Texture HistogramTexture { get; private set; }
        #endregion

        #region Constructors
        public FMRI()
        {
            Name = "Default";
            Volumes.Add(new Volume());
        }
        public FMRI(Data.MRI mri, Data.MRI mask, bool loadInBackground = true) : this(mri.Name, mri.File, mask.File, loadInBackground)
        {
        }
        public FMRI(string name, string file, string maskFile = "", bool loadInBackground = true)
        {
            Name = name;
            m_File = file;
            m_MaskFile = maskFile;
            if (loadInBackground)
                Load(file, maskFile).Forget();
        }
        #endregion

        #region Private Methods
        private async UniTaskVoid Load(string file, string maskFile)
        {
            await LoadAsync(file, maskFile);
        }
        /// <summary>
        /// Load the FMRI
        /// </summary>
        private async UniTask LoadAsync(string file, string maskFile)
        {
            await UniTask.SwitchToThreadPool();
            Loading = true;
            // FILE
            var nifti = new NIFTI();
            nifti.Load(file);
            for (int i = 0; i < nifti.NumberOfVolumes; i++)
            {
                Volumes.Add(nifti.ExtractVolume(i));
            }
            ExtremeValues = nifti.ExtremeValues;
            HistogramTexture = Texture.GenerateDistributionHistogram(nifti, 440, 440, false);
            if (nifti.NumberOfVolumes > 0)
            {
                StartTime = nifti.StartTime;
                TimeStep = nifti.TimeStep;
                TimeUnit = nifti.TimeUnit;
            }
            // MASK
            if (!string.IsNullOrEmpty(maskFile))
            {
                MaskVolume = new Volume();
                MaskVolume.LoadNIFTIFile(maskFile);
                if (!MaskVolume.BoundingBox.Compare(Volumes[0].BoundingBox))
                {
                    throw new HBPException("Mask and fMRI bounding box mismatch", $"The mask {maskFile} does not have the same bounding box as the fMRI {file}.");
                }
            }
            Loading = false;
            Loaded = true;
        }
        #endregion

        #region Public Methods
        public async UniTask LoadAsync()
        {
            await LoadAsync(m_File, m_MaskFile);
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
        }
        #endregion
    }
}