using CielaSpike;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HBP.Module3D.IBC
{
    public class IBCObjects : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Contains information about labels of the contrasts
        /// </summary>
        public IBCInformation Information { get; private set; }
        /// <summary>
        /// List of the contrasts of the IBC
        /// </summary>
        public List<Contrast> Contrasts { get; private set; }

        private bool m_Loaded = false;
        /// <summary>
        /// Are the constrats loaded ?
        /// </summary>
        public bool Loaded
        {
            get
            {
                return m_Loaded && Contrasts.Count > 0;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            this.StartCoroutineAsync(c_LoadContrasts());
        }
        private void OnDestroy()
        {
            foreach (var contrast in Contrasts)
            {
                contrast.Clean();
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator c_LoadContrasts()
        {
            yield return Ninja.JumpToUnity;

            string directory = ApplicationState.DataPath + "IBC/";
            string csvFile = ApplicationState.DataPath + "IBC/map_labels.csv";
            string[] files = Directory.GetFiles(directory, "*.nii.gz");

            yield return Ninja.JumpBack;

            Contrasts = new List<Contrast>(files.Length);
            Information = new IBCInformation(csvFile);
            foreach (var file in files)
            {
                Contrasts.Add(new Contrast(file, Information));
            }

            yield return Ninja.JumpToUnity;
            m_Loaded = true;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}