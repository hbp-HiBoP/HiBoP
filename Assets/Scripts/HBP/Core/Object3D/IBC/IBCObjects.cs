using System.IO;
using HBP.Core.Tools;

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
        public bool Loaded => FMRI != null && Information != null && FMRI.Loaded && Information.Loaded;
        public bool Loading => FMRI != null && Information != null && (FMRI.Loading || Information.Loading);

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
            string csvFile = Path.Combine(ApplicationState.DataPath, "Atlases", "IBC", "map_labels.csv");
            string file = Path.Combine(ApplicationState.DataPath, "Atlases", "IBC", "all_maps.nii.gz");
            FMRI = new FMRI("IBC", file);
            Information = new IBCInformation(csvFile);
        }

        #endregion
    }
}
