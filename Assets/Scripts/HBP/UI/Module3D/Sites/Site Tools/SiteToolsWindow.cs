using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// This class defines the object used to apply an action on the filtered sites (change their respective states or export information about them in a csv file)
    /// </summary>
    public class SiteToolsWindow : Window
    {
        #region Properties
        private Base3DScene m_Scene;
        /// <summary>
        /// Associated 3D scene
        /// </summary>
        public Base3DScene Scene
        {
            get => m_Scene;
            set
            {
                m_Scene = value;
                foreach (var section in m_SiteToolSections) section.Scene = value;
            }
        }

        [SerializeField] private SiteToolSection[] m_SiteToolSections;

        [SerializeField] private Dropdown m_SelectToolDropdown;
        [SerializeField] private Dropdown m_ApplyForDropdown;

        /// <summary>
        /// Button to trigger the application of the action
        /// </summary>
        [SerializeField] private Button m_ApplyChangesButton;

        static int m_SelectToolDropdownValue;
        static int m_ApplyForDropdownValue;
        #endregion

        #region Events
        /// <summary>
        /// Event called when requesting an update in the sites list
        /// </summary>
        public UnityEvent OnToolApplied = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Apply the configured action to the filitered sites
        /// </summary>
        public async void Apply()
        {
            await m_SiteToolSections[m_SelectToolDropdown.value].ApplyAsync();
            await UniTask.SwitchToMainThread();
            OnToolApplied.Invoke();
        }
        public override void Close()
        {
            base.Close();

            StoreSettings();
            foreach (var section in m_SiteToolSections) section.StoreSettings();
        }
        public void StoreSettings()
        {
            m_SelectToolDropdownValue = m_SelectToolDropdown.value;
            m_ApplyForDropdownValue = m_ApplyForDropdown.value;
        }
        public void LoadSettings()
        {
            m_SelectToolDropdown.SetValue(m_SelectToolDropdownValue);
            m_ApplyForDropdown.Set(typeof(ApplyFor), m_ApplyForDropdownValue);
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_ApplyChangesButton.onClick.AddListener(Apply);
            m_SelectToolDropdown.onValueChanged.AddListener(OnChangeSelectedTool);
            m_ApplyForDropdown.onValueChanged.AddListener(OnChangeApplyFor);
            foreach (var section in m_SiteToolSections) section.Initialize();

            base.Initialize();
        }
        protected override void SetFields()
        {
            base.SetFields();
            LoadSettings();
        }
        private void OnChangeSelectedTool(int index)
        {
            foreach (var section in m_SiteToolSections) section.gameObject.SetActive(false);
            m_SiteToolSections[index].gameObject.SetActive(true);
        }
        private void OnChangeApplyFor(int index)
        {
            foreach (var section in m_SiteToolSections) section.ApplyFor = (ApplyFor)index;
        }
        #endregion
    }

    public enum ApplyFor { FilteredSites, AllSites }
}