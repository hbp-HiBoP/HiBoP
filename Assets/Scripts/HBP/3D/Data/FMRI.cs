using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
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
        public bool Loaded { get; private set; } = false;
        /// <summary>
        /// Data of the MRI (paths etc.)
        /// </summary>
        private Data.MRI m_MRI;
        #endregion

        #region Constructors
        public FMRI(Data.MRI mri)
        {
            Name = mri.Name;
            m_MRI = mri;
            Load();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the FMRI
        /// </summary>
        public void Load()
        {
            NIFTI.Load(m_MRI.File);
            for (int i = 0; i < NIFTI.NumberOfVolumes; i++)
            {
                Volumes.Add(NIFTI.ExtractVolume(i));
            }
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