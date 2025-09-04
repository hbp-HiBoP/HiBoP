using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
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
        public List<Volume> Volumes { get; private set; } = new List<Volume>();
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
            var nifti = new NIFTI();
            nifti.Load(file);
            for (int i = 0; i < nifti.NumberOfVolumes; i++)
            {
                Volumes.Add(nifti.ExtractVolume(i));
            }
            ExtremeValues = nifti.ExtremeValues;
            UnityEngine.Debug.Log($"Extreme values: {ExtremeValues.Min}, {ExtremeValues.Max}");
            HistogramTexture = Texture.GenerateDistributionHistogram(nifti, 440, 440, false);
            if (nifti.NumberOfVolumes > 0)
            {
                StartTime = nifti.StartTime;
                TimeStep = nifti.TimeStep;
                TimeUnit = nifti.TimeUnit;
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
        }
        #endregion
    }
}