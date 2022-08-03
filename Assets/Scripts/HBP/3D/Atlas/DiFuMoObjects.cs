using ThirdParty.CielaSpike;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HBP.Core.Object3D
{
    public class DiFuMoObjects : MonoBehaviour
    {
        #region Properties
        public Dictionary<string, DiFuMoInformation> Information { get; private set; } = new Dictionary<string, DiFuMoInformation>();
        public Dictionary<string, FMRI> FMRIs { get; private set; } = new Dictionary<string, FMRI>();
        public bool Loaded { get { return FMRIs.Count > 0; } }
        private Dictionary<string, bool> m_Loading = new Dictionary<string, bool>() { { "64", false }, { "128", false }, { "256", false }, { "512", false }, { "1024", false } };
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo64) Load("64");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo128) Load("128");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo256) Load("256");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo512) Load("512");
            if (ApplicationState.UserPreferences.Data.Atlases.PreloadDiFuMo1024) Load("1024");
        }
        private void OnDestroy()
        {
            foreach (var fmri in FMRIs.Values)
            {
                fmri?.Clean();
            }
        }
        #endregion

        #region Public Methods
        public void Load(string atlas)
        {
            this.StartCoroutineAsync(c_LoadDiFuMo(atlas));
        }
        public bool IsLoaded(string atlas)
        {
            return FMRIs.Keys.Any(a => a == atlas);
        }
        public bool IsLoading(string atlas)
        {
            return m_Loading[atlas];
        }
        #endregion

        #region Coroutines
        private IEnumerator c_LoadDiFuMo(string name)
        {
            yield return Ninja.JumpToUnity;
            
            m_Loading[name] = true;
            string csvFile = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", name, string.Format("labels_{0}_dictionary.csv", name));
            string file = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", name, "maps.nii.gz");

            yield return Ninja.JumpBack;
            FMRIs.Add(name, new FMRI(name, file));
            Information.Add(name, new DiFuMoInformation(csvFile));
            yield return Ninja.JumpToUnity;

            m_Loading[name] = false;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}