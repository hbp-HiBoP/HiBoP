using CielaSpike;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D.IBC
{
    /// <summary>
    /// Class containing the data of the IBC functional atlas
    /// </summary>
    public class IBCObjects : MonoBehaviour
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
        private void Awake()
        {
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadIBC) Load();
        }
        private void OnDestroy()
        {
            FMRI?.Clean();
        }
        #endregion

        #region Public Methods
        public void Load()
        {
            this.StartCoroutineAsync(c_LoadIBC());
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Load all the contrast that are in the IBC directory
        /// </summary>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadIBC()
        {
            yield return Ninja.JumpToUnity;

            Loading = true;
            string csvFile = Path.Combine(ApplicationState.DataPath, "Atlases", "IBC", "map_labels.csv");
            string file = Path.Combine(ApplicationState.DataPath, "Atlases", "IBC", "all_maps.nii.gz");

            yield return Ninja.JumpBack;
            FMRI = new FMRI("IBC", file);
            Information = new IBCInformation(csvFile);
            yield return Ninja.JumpToUnity;

            Loading = false;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}