using CielaSpike;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D.DiFuMo
{
    public class DiFuMoObjects : MonoBehaviour
    {
        #region Properties
        public DiFuMoInformation Information { get; private set; }
        public FMRI FMRI { get; private set; }
        public bool Loaded { get; private set; } = false;
        #endregion

        #region Private Methods
        private void Awake()
        {
            this.StartCoroutineAsync(c_LoadDiFuMo());
        }
        private void OnDestroy()
        {
            FMRI?.Clean();
        }
        #endregion
        
        #region Coroutines
        private IEnumerator c_LoadDiFuMo() // Only 256 for now
        {
            yield return Ninja.JumpToUnity;

            string directory = ApplicationState.DataPath + "Atlases/DiFuMo/256";
            string csvFile = ApplicationState.DataPath + "Atlases/DiFuMo/256/labels_256_dictionary.csv";
            string file = ApplicationState.DataPath + "Atlases/DiFuMo/256/maps.nii.gz";

            yield return Ninja.JumpBack;

            Data.MRI mri = new Data.MRI("DiFuMo256", file);
            FMRI = new FMRI(mri);
            Information = new DiFuMoInformation(csvFile);

            yield return Ninja.JumpToUnity;
            Loaded = true;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}