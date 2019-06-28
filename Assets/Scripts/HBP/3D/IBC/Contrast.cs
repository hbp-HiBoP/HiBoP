using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HBP.Module3D.IBC
{
    public class Contrast
    {
        #region Properties
        /// <summary>
        /// Raw name of the contrast
        /// </summary>
        public string RawName { get; private set; }
        /// <summary>
        /// Name of the contrast
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Control condition of the contrast (contrast is target-control)
        /// </summary>
        public string ControlCondition { get; private set; }
        /// <summary>
        /// Target condition of the contrast (contrast is target-control)
        /// </summary>
        public string TargetCondition { get; private set; }
        /// <summary>
        /// Nifti image of the contrast
        /// </summary>
        public DLL.NIFTI NIFTI { get; private set; }
        /// <summary>
        /// Volume of the contrast
        /// </summary>
        public DLL.Volume Volume { get; private set; }
        /// <summary>
        /// Is the contrast loaded ?
        /// </summary>
        public bool Loaded { get; private set; }
        #endregion

        #region Constructors
        public Contrast(string file, IBCInformation information)
        {
            FileInfo fileInfo = new FileInfo(file);
            if (fileInfo.Exists)
            {
                RawName = fileInfo.Name.Replace("map_", "").Replace(".nii.gz", "");
                IBCInformation.Labels labels = information.GetLabels(RawName);
                Name = labels.PrettyName;
                ControlCondition = labels.ControlCondition;
                TargetCondition = labels.TargetCondition;
                NIFTI = new DLL.NIFTI();
                if (NIFTI.LoadNIIFile(file))
                {
                    Volume = new DLL.Volume();
                    NIFTI.ConvertToVolume(Volume);
                    Loaded = true;
                }
            }
        }
        #endregion

        #region Public Methods
        public void Clean()
        {
            NIFTI?.Dispose();
            Volume?.Dispose();
        }
        #endregion
    }
}
