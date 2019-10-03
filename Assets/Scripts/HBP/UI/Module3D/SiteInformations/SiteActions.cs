using CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteActions : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;
        private Coroutine m_Coroutine;

        [SerializeField] private SiteConditionsProgressBar m_ProgressBar;

        [SerializeField] private Toggle m_OnOffToggle;
        [SerializeField] private GameObject m_ActionsPanel;

        [SerializeField] private Toggle m_ExportSitesToggle;
        [SerializeField] private GameObject m_ExportSitesPanel;
        [SerializeField] private Toggle m_ChangeStateToggle;
        [SerializeField] private GameObject m_ChangeStatePanel;

        [SerializeField] private Toggle m_Highlight;
        [SerializeField] private Toggle m_Unhighlight;
        [SerializeField] private Toggle m_Blacklist;
        [SerializeField] private Toggle m_Unblacklist;
        [SerializeField] private Toggle m_ColorToggle;
        [SerializeField] private Button m_ColorPickerButton;
        [SerializeField] private Image m_ColorPickedImage;
        [SerializeField] private Toggle m_AddLabelToggle;
        [SerializeField] private Toggle m_RemoveLabelToggle;
        [SerializeField] private InputField m_LabelInputField;
        [SerializeField] private Toggle m_AllColumnsToggle;

        [SerializeField] private Button m_ApplyButton;

        private bool m_UpdateUI = true;
        #endregion

        #region Events
        public UnityEvent OnRequestListUpdate = new UnityEvent();
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
        }
        public void ApplyAction()
        {
            try
            {
                if (m_ChangeStateToggle.isOn)
                {
                    ChangeSitesStates();
                }
                else if (m_ExportSitesToggle.isOn)
                {
                    if (m_Coroutine != null)
                    {
                        StopCoroutine(m_Coroutine);
                        StopExport();
                    }
                    else
                    {
                        ExportSites();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, e.ToString(), e.Message);
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_OnOffToggle.onValueChanged.AddListener(m_ActionsPanel.gameObject.SetActive);
            m_ExportSitesToggle.onValueChanged.AddListener(m_ExportSitesPanel.SetActive);
            m_ChangeStateToggle.onValueChanged.AddListener(m_ChangeStatePanel.SetActive);
            m_ApplyButton.onClick.AddListener(ApplyAction);
            m_ColorPickerButton.onClick.AddListener(() =>
            {
                ApplicationState.Module3DUI.ColorPicker.Open(m_ColorPickedImage.color, (c) => m_ColorPickedImage.color = c);
            });
        }
        private void Update()
        {
            m_UpdateUI = true;
        }
        private void ChangeSitesStates()
        {
            List<Site> sites = new List<Site>();
            if (m_AllColumnsToggle.isOn)
            {
                foreach (var column in m_Scene.Columns)
                {
                    sites.AddRange(column.Sites.Where(s => s.State.IsFiltered));
                }
            }
            else
            {
                sites.AddRange(m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered));
            }

            foreach (var site in sites)
            {
                if (m_Highlight.isOn) site.State.IsHighlighted = true;
                if (m_Unhighlight.isOn) site.State.IsHighlighted = false;
                if (m_Blacklist.isOn) site.State.IsBlackListed = true;
                if (m_Unblacklist.isOn) site.State.IsBlackListed = false;
                if (m_ColorToggle.isOn) site.State.Color = m_ColorPickedImage.color;
                if (m_AddLabelToggle.isOn) site.State.AddLabel(m_LabelInputField.text);
                if (m_RemoveLabelToggle.isOn) site.State.RemoveLabel(m_LabelInputField.text);
            }

            OnRequestListUpdate.Invoke();
        }
        private void ExportSites()
        {
            string csvPath = "";
            csvPath = FileBrowser.GetSavedFileName(new string[] { "csv" }, "Save sites to", Application.dataPath);
            if (string.IsNullOrEmpty(csvPath)) return;

            m_ProgressBar.Begin();
            List<Site> sites = m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered).ToList();
            m_Coroutine = this.StartCoroutineAsync(c_ExportSites(sites, csvPath));
        }
        private void StopExport()
        {
            m_Coroutine = null;
            m_ProgressBar.End();
        }
        #endregion

        #region Coroutines
        private IEnumerator c_ExportSites(List<Site> sites, string csvPath)
        {
            int length = sites.Count;

            // Prepare DataInfo by Patient for performance increase
            Dictionary<Data.Patient, Data.Experience.Dataset.DataInfo>  dataInfoByPatient = new Dictionary<Data.Patient, Data.Experience.Dataset.DataInfo>();
            for (int i = 0; i < length; i++)
            {
                Site site = sites[i];
                if (!dataInfoByPatient.ContainsKey(site.Information.Patient))
                {
                    if (m_Scene.SelectedColumn is Column3DIEEG columnIEEG)
                    {
                        Data.Experience.Dataset.DataInfo dataInfo = m_Scene.Visualization.GetDataInfo(site.Information.Patient, columnIEEG.ColumnIEEGData);
                        dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                    }
                    else if (m_Scene.SelectedColumn is Column3DCCEP columnCCEP)
                    {
                        Data.Experience.Dataset.DataInfo dataInfo = m_Scene.Visualization.GetDataInfo(site.Information.Patient, columnCCEP.ColumnCCEPData);
                        dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                    }
                }
                // Update progressbar
                if (m_UpdateUI || i == length - 1)
                {
                    yield return Ninja.JumpToUnity;
                    m_ProgressBar.Progress(0.5f * ((float)(i + 1) / length));
                    m_UpdateUI = false;
                    yield return Ninja.JumpBack;
                }
            }

            // Create string builder
            System.Text.StringBuilder csvBuilder = new System.Text.StringBuilder();
            csvBuilder.AppendLine("Site,Patient,Place,Date,X,Y,Z,CoordSystem,Labels,DataType,DataFiles");

            // Prepare sites positions for performance increase
            yield return Ninja.JumpToUnity;
            List<Vector3> sitePositions = sites.Select(s => s.transform.localPosition).ToList();
            yield return Ninja.JumpBack;

            for (int i = 0; i < length; i++)
            {
                // Get required values
                Site site = sites[i];
                Vector3 sitePosition = sitePositions[i];
                Data.Experience.Dataset.DataInfo dataInfo = null;
                if (m_Scene.SelectedColumn is Column3DDynamic columnIEEG)
                {
                    dataInfo = dataInfoByPatient[site.Information.Patient];
                }
                string dataType = "", dataFiles = "";
                if (dataInfo != null)
                {
                    if (dataInfo.DataContainer is Data.Container.BrainVision brainVisionDataContainer)
                    {
                        dataType = "BrainVision";
                        dataFiles = string.Join(";", new string[] { brainVisionDataContainer.Header }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (dataInfo.DataContainer is Data.Container.EDF edfDataContainer)
                    {
                        dataType = "EDF";
                        dataFiles = string.Join(";", new string[] { edfDataContainer.Path }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (dataInfo.DataContainer is Data.Container.Elan elanDataContainer)
                    {
                        dataType = "ELAN";
                        dataFiles = string.Join(";", new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (dataInfo.DataContainer is Data.Container.Micromed micromedDataContainer)
                    {
                        dataType = "Micromed";
                        dataFiles = string.Join(";", new string[] { micromedDataContainer.Path }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else
                    {
                        throw new Exception("Invalid data container type");
                    }
                }
                // Write in string builder
                csvBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                        site.Information.ChannelName,
                        site.Information.Patient.Name,
                        site.Information.Patient.Place,
                        site.Information.Patient.Date,
                        sitePosition.x.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.y.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.z.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        m_Scene.ImplantationManager.SelectedImplantation.Name,
                        string.Join(";", site.State.Labels),
                        dataType,
                        dataFiles));
                // Update progressbar
                if (m_UpdateUI || i == length - 1)
                {
                    yield return Ninja.JumpToUnity;
                    m_ProgressBar.Progress(0.5f * (1 + (float)(i + 1) / length));
                    m_UpdateUI = false;
                    yield return Ninja.JumpBack;
                }
            }

            // Write csv file
            yield return Ninja.JumpBack;
            try
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(csvPath))
                {
                    sw.Write(csvBuilder.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, e.ToString(), e.Message);
                yield break;
            }
            yield return Ninja.JumpToUnity;

            // End
            StopExport();
            ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Informational, "Sites exported", "The filtered sites have been sucessfully exported to " + csvPath);
            OnRequestListUpdate.Invoke();
        }
        #endregion
    }
}