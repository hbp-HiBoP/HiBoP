﻿using CielaSpike;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private List<Contrast> m_Contrasts;

        public List<Contrast> Contrasts
        {
            get
            {
                return m_Contrasts.Where(c => c.Loaded).ToList();
            }
        }

        private bool m_Loaded = false;
        /// <summary>
        /// Are the constrats loaded ?
        /// </summary>
        public bool Loaded
        {
            get
            {
                return m_Loaded && m_Contrasts.Count > 0;
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
            foreach (var contrast in m_Contrasts)
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

            m_Contrasts = new List<Contrast>(files.Length);
            Information = new IBCInformation(csvFile);
            foreach (var file in files)
            {
                m_Contrasts.Add(new Contrast(file, Information));
            }

            yield return Ninja.JumpToUnity;
            m_Loaded = true;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}