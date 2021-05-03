﻿using CielaSpike;
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
        public bool Loaded { get; private set; } = false;
        #endregion

        #region Private Methods
        private void Awake()
        {
            this.StartCoroutineAsync(c_LoadIBC());
        }
        private void OnDestroy()
        {
            FMRI?.Clean();
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

            string directory = ApplicationState.DataPath + "Atlases/IBC/";
            string csvFile = ApplicationState.DataPath + "Atlases/IBC/map_labels.csv";
            string file = ApplicationState.DataPath + "Atlases/IBC/all_maps.nii.gz";

            yield return Ninja.JumpBack;

            Data.MRI mri = new Data.MRI("IBC", file);
            FMRI = new FMRI(mri);
            Information = new IBCInformation(csvFile);

            yield return Ninja.JumpToUnity;
            Loaded = true;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}