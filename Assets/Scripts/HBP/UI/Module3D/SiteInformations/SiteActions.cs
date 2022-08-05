using ThirdParty.CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Tools.Unity;
using HBP.Core.Data;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// This class defines the object used to apply an action on the filtered sites (change their respective states or export information about them in a csv file)
    /// </summary>
    public class SiteActions : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Associated 3D scene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Currently computing coroutine (for the export of the sites to a csv file)
        /// </summary>
        private Coroutine m_Coroutine;

        /// <summary>
        /// Progress bar used to display a feedback when filtering or applying an action
        /// </summary>
        [SerializeField] private SiteConditionsProgressBar m_ProgressBar;

        /// <summary>
        /// Toggle used to display or hide the actions panel
        /// </summary>
        [SerializeField] private Toggle m_OnOffToggle;
        /// <summary>
        /// Object containing all the buttons to apply an action to the filtered sites
        /// </summary>
        [SerializeField] private GameObject m_ActionsPanel;

        /// <summary>
        /// Toggle to specify that the action to be apply is a change in the states of the sites
        /// </summary>
        [SerializeField] private Toggle m_ChangeStateToggle;
        /// <summary>
        /// Object containing all the UI used to change the states of the sites
        /// </summary>
        [SerializeField] private GameObject m_ChangeStatePanel;
        /// <summary>
        /// Toggle to specify that the action to be apply is a misc action
        /// </summary>
        [SerializeField] private Toggle m_OtherToggle;
        /// <summary>
        /// Object containing all the UI used for misc actions
        /// </summary>
        [SerializeField] private GameObject m_OtherPanel;

        /// <summary>
        /// Used to highlight the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_Highlight;
        /// <summary>
        /// Used to unhighlight the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_Unhighlight;
        /// <summary>
        /// Used to blacklist the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_Blacklist;
        /// <summary>
        /// Used to unblacklist the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_Unblacklist;
        /// <summary>
        /// Used to change the color of the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_ColorToggle;
        /// <summary>
        /// Used to select the color to be applied to the selected sites
        /// </summary>
        [SerializeField] private Button m_ColorPickerButton;
        /// <summary>
        /// Image to show the selected color
        /// </summary>
        [SerializeField] private Image m_ColorPickedImage;
        /// <summary>
        /// Used to add a label to the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_AddLabelToggle;
        /// <summary>
        /// Used to remove a label from the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_RemoveLabelToggle;
        /// <summary>
        /// InputField used to type in the label to be added to or removed from the filtered sites
        /// </summary>
        [SerializeField] private InputField m_LabelInputField;
        /// <summary>
        /// Used to remove all labels from the filtered sites
        /// </summary>
        [SerializeField] private Toggle m_RemoveAllLabelsToggle;
        /// <summary>
        /// Toggle to choose to apply the modifications in the states of the filtered sites to the selected column or to all columns at once
        /// </summary>
        [SerializeField] private Toggle m_AllColumnsToggle;

        /// <summary>
        /// Toggle to specify that the action to be apply is an export to a csv file
        /// </summary>
        [SerializeField] private Toggle m_ExportSitesToggle;
        /// <summary>
        /// Toggle to specify that the action to be apply is a group creation
        /// </summary>
        [SerializeField] private Toggle m_CreateGroupToggle;
        /// <summary>
        /// Inputfield to specify the name of the newly created group
        /// </summary>
        [SerializeField] private InputField m_GroupNameInputField;
        /// <summary>
        /// Toggle to specify that the action to be apply is a graph display
        /// </summary>
        [SerializeField] private Toggle m_DisplayGraphsToggle;

        /// <summary>
        /// Button to trigger the application of the action
        /// </summary>
        [SerializeField] private Button m_ApplyButton;

        /// <summary>
        /// Do we need an update in the progress bar ?
        /// </summary>
        private bool m_UpdateUI = true;
        #endregion

        #region Events
        /// <summary>
        /// Event called when requesting an update in the sites list
        /// </summary>
        public UnityEvent OnRequestListUpdate = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this object
        /// </summary>
        /// <param name="scene">Associated 3D scene</param>
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
        }
        /// <summary>
        /// Apply the configured action to the filitered sites
        /// </summary>
        public void ApplyAction()
        {
            try
            {
                if (m_ChangeStateToggle.isOn)
                {
                    ChangeSitesStates();
                }
                else if (m_OtherToggle.isOn)
                {
                    if (m_ExportSitesToggle.isOn)
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
                    else if (m_CreateGroupToggle.isOn)
                    {
                        CreateGroup();
                    }
                    else if (m_DisplayGraphsToggle.isOn)
                    {
                        DisplayGraphs();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, e.ToString(), e.Message);
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_OnOffToggle.onValueChanged.AddListener(m_ActionsPanel.gameObject.SetActive);
            m_ChangeStateToggle.onValueChanged.AddListener(m_ChangeStatePanel.SetActive);
            m_OtherToggle.onValueChanged.AddListener(m_OtherPanel.SetActive);
            m_ApplyButton.onClick.AddListener(ApplyAction);

            m_ColorPickerButton.onClick.AddListener(() =>
            {
                ColorPicker.Open(m_ColorPickedImage.color, (c) => m_ColorPickedImage.color = c);
            });
            m_AddLabelToggle.onValueChanged.AddListener(isOn => m_LabelInputField.interactable = m_AddLabelToggle.isOn || m_RemoveLabelToggle.isOn);
            m_RemoveLabelToggle.onValueChanged.AddListener(isOn => m_LabelInputField.interactable = m_AddLabelToggle.isOn || m_RemoveLabelToggle.isOn);
            m_ColorToggle.onValueChanged.AddListener(isOn => m_ColorPickerButton.interactable = isOn);
            m_CreateGroupToggle.onValueChanged.AddListener(isOn => m_GroupNameInputField.interactable = isOn);
        }
        private void Update()
        {
            m_UpdateUI = true;
        }
        /// <summary>
        /// Change the states of the filtered sites
        /// </summary>
        private void ChangeSitesStates()
        {
            List<Core.Object3D.Site> sites = new List<Core.Object3D.Site>();
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
                if (m_RemoveAllLabelsToggle.isOn) site.State.RemoveAllLabels();
            }

            OnRequestListUpdate.Invoke();
        }
        /// <summary>
        /// Export the filtered sites to a csv file
        /// </summary>
        private void ExportSites()
        {
#if UNITY_STANDALONE_OSX
            FileBrowser.GetSavedFileNameAsync((csvPath) =>
            {
                if (string.IsNullOrEmpty(csvPath)) return;

                m_ProgressBar.Begin();
                List<Core.Object3D.Site> sites = m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered).ToList();
                m_Coroutine = this.StartCoroutineAsync(c_ExportSites(sites, csvPath));
            }, new string[] { "csv" }, "Save sites to", Application.dataPath);
#else
            string csvPath = FileBrowser.GetSavedFileName(new string[] { "csv" }, "Save sites to", Application.dataPath);
            if (string.IsNullOrEmpty(csvPath)) return;

            m_ProgressBar.Begin();
            List<Core.Object3D.Site> sites = m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered).ToList();
            m_Coroutine = this.StartCoroutineAsync(c_ExportSites(sites, csvPath));
#endif
        }
        /// <summary>
        /// Create a group from all patients of filtered sites
        /// </summary>
        private void CreateGroup()
        {
            var patients = m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered).Select(s => s.Information.Patient).Distinct();
            Core.Data.Group group = new Core.Data.Group(m_GroupNameInputField.text, patients);
            // Generate unique name
            var projectGroups = ApplicationState.ProjectLoaded.Groups;
            if (projectGroups.Any(g => g.Name == group.Name))
            {
                int count = 1;
                string name = string.Format("{0}({1})", group.Name, count);
                while (projectGroups.Any(g => g.Name == name))
                {
                    count++;
                    name = string.Format("{0}({1})", group.Name, count);
                }
                group.Name = name;
            }
            ApplicationState.ProjectLoaded.AddGroup(group);
            DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Informational, "Group added to project", string.Format("The group {0} containing the {1} patients of the filtered sites has been added to the project.", group.Name, patients.Count()));
        }
        /// <summary>
        /// Cancel the export of the filtered sites
        /// </summary>
        private void StopExport()
        {
            m_Coroutine = null;
            m_ProgressBar.End();
        }
        private void DisplayGraphs()
        {
            m_Scene.OnRequestFilteredSitesGraph.Invoke(m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered));
        }
#endregion

#region Coroutines
        /// <summary>
        /// Coroutine used to export the filtered sites to a csv file in asynchronous mode
        /// </summary>
        /// <param name="sites">List of the sites to export</param>
        /// <param name="csvPath">Path to the csv file</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_ExportSites(List<Core.Object3D.Site> sites, string csvPath)
        {
            int length = sites.Count;

            // Prepare DataInfo by Patient for performance increase
            Dictionary<Core.Data.Patient, Core.Data.DataInfo>  dataInfoByPatient = new Dictionary<Core.Data.Patient, Core.Data.DataInfo>();
            for (int i = 0; i < length; i++)
            {
                Core.Object3D.Site site = sites[i];
                if (!dataInfoByPatient.ContainsKey(site.Information.Patient))
                {
                    if (m_Scene.SelectedColumn is Column3DIEEG columnIEEG)
                    {
                        Core.Data.DataInfo dataInfo = m_Scene.Visualization.GetDataInfo(site.Information.Patient, columnIEEG.ColumnIEEGData);
                        dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                    }
                    else if (m_Scene.SelectedColumn is Column3DCCEP columnCCEP)
                    {
                        Core.Data.DataInfo dataInfo = m_Scene.Visualization.GetDataInfo(site.Information.Patient, columnCCEP.ColumnCCEPData);
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
            string tagsString = "";
            IEnumerable<Core.Data.BaseTag> tags = ApplicationState.ProjectLoaded.Preferences.GeneralTags.Concat(ApplicationState.ProjectLoaded.Preferences.SitesTags);
            if (tags.Count() > 0) tagsString = string.Format(",{0}", string.Join(",", tags.Select(t => !t.Name.Contains(",") ? t.Name : string.Format("\"{0}\"", t.Name))));
            csvBuilder.AppendLine("Site,Patient,Place,Date,X,Y,Z,CoordSystem,Labels,DataType,DataFiles" + tagsString);

            // Prepare sites positions for performance increase
            yield return Ninja.JumpToUnity;
            List<Vector3> sitePositions = sites.Select(s => s.transform.localPosition).ToList();
            yield return Ninja.JumpBack;

            for (int i = 0; i < length; i++)
            {
                // Get required values
                Core.Object3D.Site site = sites[i];
                Vector3 sitePosition = sitePositions[i];
                Core.Data.DataInfo dataInfo = null;
                if (m_Scene.SelectedColumn is Column3DDynamic columnIEEG)
                {
                    dataInfo = dataInfoByPatient[site.Information.Patient];
                }
                string dataType = "", dataFiles = "";
                if (dataInfo != null)
                {
                    if (dataInfo.DataContainer is Core.Data.Container.BrainVision brainVisionDataContainer)
                    {
                        dataType = "BrainVision";
                        dataFiles = string.Join(";", new string[] { brainVisionDataContainer.Header }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (dataInfo.DataContainer is Core.Data.Container.EDF edfDataContainer)
                    {
                        dataType = "EDF";
                        dataFiles = string.Join(";", new string[] { edfDataContainer.File }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (dataInfo.DataContainer is Core.Data.Container.Elan elanDataContainer)
                    {
                        dataType = "ELAN";
                        dataFiles = string.Join(";", new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (dataInfo.DataContainer is Core.Data.Container.Micromed micromedDataContainer)
                    {
                        dataType = "Micromed";
                        dataFiles = string.Join(";", new string[] { micromedDataContainer.Path }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else if (dataInfo.DataContainer is Core.Data.Container.FIF fifDataContainer)
                    {
                        dataType = "FIF";
                        dataFiles = string.Join(";", new string[] { fifDataContainer.File }.Where(s => !string.IsNullOrEmpty(s)));
                    }
                    else
                    {
                        throw new Exception("Invalid data container type");
                    }
                }
                IEnumerable<Core.Data.BaseTagValue> tagValues = tags.Select(t => site.Information.SiteData.Tags.FirstOrDefault(tv => tv.Tag == t));
                string tagValuesString = "";
                if (tagValues.Count() > 0)
                {
                    System.Text.StringBuilder tagValuesStringBuilder = new System.Text.StringBuilder();
                    foreach (var tagValue in tagValues)
                    {
                        tagValuesStringBuilder.Append(",");
                        if (tagValue != null)
                        {
                            string value = tagValue.DisplayableValue;
                            if (value.Contains(","))
                            {
                                value = string.Format("\"{0}\"", value);
                            }
                            tagValuesStringBuilder.Append(value);
                        }
                    }
                    tagValuesString = tagValuesStringBuilder.ToString();
                }
                // Write in string builder
                csvBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}{11}",
                        site.Information.Name,
                        site.Information.Patient.Name,
                        site.Information.Patient.Place,
                        site.Information.Patient.Date,
                        sitePosition.x.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.y.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        sitePosition.z.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                        m_Scene.ImplantationManager.SelectedImplantation.Name,
                        string.Join(";", site.State.Labels),
                        dataType,
                        dataFiles,
                        tagValuesString));
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
                DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, e.ToString(), e.Message);
                yield break;
            }
            yield return Ninja.JumpToUnity;

            // End
            StopExport();
            DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Informational, "Sites exported", "The filtered sites have been sucessfully exported to " + csvPath);
            OnRequestListUpdate.Invoke();
        }
#endregion
    }
}