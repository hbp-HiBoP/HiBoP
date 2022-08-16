using System.IO;
using HBP.Core.Data;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class containing the data of the IBC functional atlas
    /// </summary>
    public class IBCObjects
    {
        #region Properties
        /// <summary>
        /// Contains information about labels of the contrasts
        /// </summary>
        public IBCInformation Information { get; private set; }
        public FMRI FMRI { get; private set; }
        public bool Loaded { get { return FMRI != null; } }
        public bool Loading { get; private set; } = false;
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        public void Clean()
        {
            FMRI?.Clean();
        }
        public void Load()
        {
            Loading = true;
            string csvFile = Path.Combine(ApplicationState.DataPath, "Atlases", "IBC", "map_labels.csv");
            string file = Path.Combine(ApplicationState.DataPath, "Atlases", "IBC", "all_maps.nii.gz");
            FMRI = new FMRI("IBC", file);
            Information = new IBCInformation(csvFile);
            Loading = false;
        }
        #endregion
    }
}