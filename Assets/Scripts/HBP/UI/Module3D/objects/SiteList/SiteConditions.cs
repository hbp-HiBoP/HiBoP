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
    public class SiteConditions : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        [SerializeField] BasicSiteConditions m_BasicSiteConditions;
        [SerializeField] AdvancedSiteConditions m_AdvancedSiteConditions;
        Coroutine m_Coroutine;

        public bool UseAdvanced { get; set; }
        public bool AllColumns { get; set; }

        private enum ActionType { ChangeState, Export };
        private ActionType m_ActionType;
        
        [SerializeField] Toggle m_Highlight;
        [SerializeField] Toggle m_Unhighlight;
        [SerializeField] Toggle m_Blacklist;
        [SerializeField] Toggle m_Unblacklist;
        [SerializeField] Toggle m_ColorToggle;
        [SerializeField] Button m_ColorPickerButton;
        [SerializeField] Image m_ColorPickedImage;

        // Export specific variables
        private System.Text.StringBuilder m_CSVBuilder;
        private string m_SavePath;
        private Dictionary<Data.Patient, Data.Experience.Dataset.DataInfo> m_DataInfoByPatient;
        #endregion

        #region Events
        public UnityEvent OnBeginApply = new UnityEvent();
        public ApplyingActionEvent OnApplyingActions = new ApplyingActionEvent();
        public UnityEvent OnSiteFound = new UnityEvent();
        public EndApplyEvent OnEndApply = new EndApplyEvent();
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            m_BasicSiteConditions.Initialize(scene);
            m_BasicSiteConditions.OnApplyActionOnSite.AddListener(Apply);
            m_AdvancedSiteConditions.Initialize(scene);
            m_AdvancedSiteConditions.OnApplyActionOnSite.AddListener(Apply);
            OnEndApply.AddListener((finished) =>
            {
                m_Coroutine = null;
                if (finished && m_ActionType == ActionType.Export)
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(m_SavePath))
                    {
                        sw.Write(m_CSVBuilder.ToString());
                    }
                    ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Informational, "Sites exported", "The filtered sites have been sucessfully exported to " + m_SavePath);
                }
            });
        }
        public void OnClickApply()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                OnEndApply.Invoke(false);
            }
            else
            {
                if (m_ActionType == ActionType.Export)
                {
                    m_SavePath = "";
                    m_SavePath = FileBrowser.GetSavedFileName(new string[] { "csv" }, "Save sites to", Application.dataPath);
                    if (string.IsNullOrEmpty(m_SavePath)) return;

                    m_CSVBuilder = new System.Text.StringBuilder();
                    m_CSVBuilder.AppendLine("Site,Patient,Place,Date,X,Y,Z,CoordSystem,DataType,DataFiles");
                }

                List<Site> sites = new List<Site>();

                if (AllColumns && m_ActionType == ActionType.ChangeState)
                {
                    foreach (var column in m_Scene.ColumnManager.Columns)
                    {
                        sites.AddRange(column.Sites);
                    }
                }
                else
                {
                    sites.AddRange(m_Scene.ColumnManager.SelectedColumn.Sites);
                }

                if (m_ActionType == ActionType.Export)
                {
                    m_DataInfoByPatient = new Dictionary<Data.Patient, Data.Experience.Dataset.DataInfo>();
                    foreach (var site in sites)
                    {
                        if (!m_DataInfoByPatient.ContainsKey(site.Information.Patient))
                        {
                            if (m_Scene.ColumnManager.SelectedColumn is Column3DIEEG columnIEEG)
                            {
                                Data.Experience.Dataset.DataInfo dataInfo = m_Scene.Visualization.GetDataInfo(site.Information.Patient, columnIEEG.ColumnIEEGData);
                                m_DataInfoByPatient.Add(site.Information.Patient, dataInfo);
                            }
                        }
                    }
                }

                try
                {
                    if (UseAdvanced)
                    {
                        m_AdvancedSiteConditions.ParseConditions();
                        m_Coroutine = this.StartCoroutineAsync(m_AdvancedSiteConditions.c_FindSitesAndRequestAction(sites, OnBeginApply, OnEndApply, OnApplyingActions, m_ActionType == ActionType.Export ? 100 : int.MaxValue));
                    }
                    else
                    {
                        m_Coroutine = this.StartCoroutineAsync(m_BasicSiteConditions.c_FindSitesAndRequestAction(sites, OnBeginApply, OnEndApply, OnApplyingActions, m_ActionType == ActionType.Export ? 100 : int.MaxValue));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, e.ToString(), e.Message);
                }
            }
        }
        public void ChangeActionType(int type)
        {
            m_ActionType = (ActionType)type;
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ColorPickerButton.onClick.AddListener(() =>
            {
                ApplicationState.Module3DUI.ColorPicker.Open(m_ColorPickedImage.color, (c) => m_ColorPickedImage.color = c);
            });
        }
        private void Apply(Site site)
        {
            switch (m_ActionType)
            {
                case ActionType.ChangeState:
                    ApplyChangeState(site);
                    break;
                case ActionType.Export:
                    ApplyExport(site);
                    break;
            }
            OnSiteFound.Invoke();
        }
        private void ApplyChangeState(Site site)
        {
            if (m_Highlight.isOn) site.State.IsHighlighted = true;
            if (m_Unhighlight.isOn) site.State.IsHighlighted = false;
            if (m_Blacklist.isOn) site.State.IsBlackListed = true;
            if (m_Unblacklist.isOn) site.State.IsBlackListed = false;
            if (m_ColorToggle.isOn) site.State.Color = m_ColorPickedImage.color;
        }
        private void ApplyExport(Site site)
        {
            Vector3 sitePosition = site.transform.localPosition;
            Data.Experience.Dataset.DataInfo dataInfo = null;
            if (m_Scene.ColumnManager.SelectedColumn is Column3DIEEG columnIEEG)
            {
                dataInfo = m_DataInfoByPatient[site.Information.Patient];
            }
            string dataType = "", dataFiles = "";
            if (dataInfo != null)
            {
                dataType = dataInfo.DataTypeString;
                dataFiles = dataInfo.DataFilesString;
            }
            m_CSVBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                    site.Information.ChannelName,
                    site.Information.Patient.Name,
                    site.Information.Patient.Place,
                    site.Information.Patient.Date,
                    sitePosition.x.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                    sitePosition.y.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                    sitePosition.z.ToString("N2", System.Globalization.CultureInfo.InvariantCulture),
                    m_Scene.ColumnManager.SelectedImplantation.Name,
                    dataType,
                    dataFiles));
        }
        #endregion
    }

    [Serializable]
    public class ApplyingActionEvent : UnityEvent<float> { }
    [Serializable]
    public class EndApplyEvent : UnityEvent<bool> { }
}