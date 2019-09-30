using System.IO;

namespace HBP.Module3D.IBC
{
    /// <summary>
    /// This class describes a contrast from the IBC functional atlas
    /// </summary>
    public class Contrast
    {
        #region Properties
        /// <summary>
        /// Raw name of the contrast (from the file name)
        /// </summary>
        public string RawName { get; private set; }
        /// <summary>
        /// Name of the contrast (from the correspondance table)
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Control condition of the contrast (contrast is target minus control)
        /// </summary>
        public string ControlCondition { get; private set; }
        /// <summary>
        /// Target condition of the contrast (contrast is target minus control)
        /// </summary>
        public string TargetCondition { get; private set; }
        /// <summary>
        /// Volume of the contrast
        /// </summary>
        public DLL.Volume Volume { get; private set; } = new DLL.Volume();
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
                Loaded = Volume.LoadNIFTIFile(file);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Dispose all DLL objects
        /// </summary>
        public void Clean()
        {
            Volume?.Dispose();
        }
        #endregion
    }
}
