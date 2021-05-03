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
        public Dictionary<string, DiFuMoInformation> Information { get; private set; } = new Dictionary<string, DiFuMoInformation>();
        public Dictionary<string, FMRI> FMRIs { get; private set; } = new Dictionary<string, FMRI>();
        public bool Loaded { get { return FMRIs.Count > 0; } }
        #endregion

        #region Private Methods
        private void Awake()
        {
            this.StartCoroutineAsync(c_LoadDiFuMo("64"));
            this.StartCoroutineAsync(c_LoadDiFuMo("256"));
        }
        private void OnDestroy()
        {
            foreach (var fmri in FMRIs.Values)
            {
                fmri?.Clean();
            }
        }
        #endregion
        
        #region Coroutines
        private IEnumerator c_LoadDiFuMo(string name)
        {
            yield return Ninja.JumpToUnity;

            string csvFile = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", name, string.Format("labels_{0}_dictionary.csv", name));
            string file = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", name, "maps.nii.gz");

            yield return Ninja.JumpBack;

            FMRIs.Add(name, new FMRI(name, file));
            Information.Add(name, new DiFuMoInformation(csvFile));

            yield return Ninja.JumpToUnity;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}