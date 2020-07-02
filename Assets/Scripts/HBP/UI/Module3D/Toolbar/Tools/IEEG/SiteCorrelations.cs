using CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SiteCorrelations : Tool
    {
        #region Properties
        /// <summary>
        /// Trigger the computation of the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Button m_Compute;
        /// <summary>
        /// Remove the projection of the iEEG activity
        /// </summary>
        [SerializeField] private Toggle m_Display;
        /// <summary>
        /// Are the correlations being computed ?
        /// </summary>
        private bool m_CorrelationsComputing = false;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Compute.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                ApplicationState.LoadingManager.Load(c_ComputeCorrelations((progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress);
            });
            m_Display.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.DisplayCorrelations = isOn;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Compute.interactable = false;
            m_Compute.gameObject.SetActive(true);
            m_Display.interactable = false;
            m_Display.isOn = false;
            m_Display.gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isSinglePatientScene = SelectedScene.Type == Data.Enums.SceneType.SinglePatient;
            bool areCorrelationsComputed = SelectedColumn is Column3DIEEG column ? column.AreCorrelationsComputed : false;

            m_Compute.interactable = !areCorrelationsComputed && !m_CorrelationsComputing && isSinglePatientScene;
            m_Compute.gameObject.SetActive(!areCorrelationsComputed);
            m_Display.interactable = areCorrelationsComputed && !m_CorrelationsComputing && isSinglePatientScene;
            m_Display.gameObject.SetActive(areCorrelationsComputed);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Display.isOn = SelectedScene.DisplayCorrelations;
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Compute correlations for all ieeg columns
        /// </summary>
        /// <param name="onChangeProgress">Action for the loading circle</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_ComputeCorrelations(Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpToUnity;
            m_CorrelationsComputing = true;
            UpdateInteractable();
            yield return Ninja.JumpBack;
            List<Column3DIEEG> columns = SelectedScene.ColumnsIEEG;
            for (int i = 0; i < columns.Count; i++)
            {
                columns[i].ComputeCorrelations((progress, duration, text) => { onChangeProgress((i + progress) / columns.Count, duration, text); } );
            }
            yield return Ninja.JumpToUnity;
            m_CorrelationsComputing = false;
            onChangeProgress(1, 0, new LoadingText("Correlations computed"));
            SelectedScene.DisplayCorrelations = true;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}