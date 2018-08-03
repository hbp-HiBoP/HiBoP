﻿using CielaSpike;
using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ExportSites : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;

        private LoadingCircle m_Circle;
        private float m_Progress;
        private string m_Message;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_Circle)
            {
                m_Circle.ChangePercentage(m_Progress, 0, m_Message);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                string savePath = HBP.Module3D.DLL.QtGUI.GetSavedFileName(new string[] { "csv" }, "Save sites to", Application.dataPath);
                if (!string.IsNullOrEmpty(savePath))
                {
                    this.StartCoroutineAsync(c_ExportSites(savePath));
                }
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool hasROI = ApplicationState.Module3D.SelectedColumn.ROIs.Count > 0;

            m_Button.interactable = hasROI;
        }
        #endregion

        #region Coroutines
        private IEnumerator c_ExportSites(string savePath)
        {
            yield return Ninja.JumpToUnity;
            m_Circle = ApplicationState.LoadingManager.Open();
            m_Progress = 0;
            m_Message = "Exporting sites";
            yield return Ninja.JumpBack;

            System.Text.StringBuilder csvBuilder = new System.Text.StringBuilder();
            csvBuilder.AppendLine("Site,Patient,Place,Date,X,Y,Z,CoordSystem,EEG,POS,MainEvent,Codes");

            Base3DScene scene = ApplicationState.Module3D.SelectedScene;
            Column3D column = scene.ColumnManager.SelectedColumn;
            List<Site> sites = column.Sites;
            int length = sites.Count;
            List<Vector3> sitePositions = new List<Vector3>();
            yield return Ninja.JumpToUnity;
            for (int i = 0; i < length; ++i) sitePositions.Add(sites[i].transform.localPosition);
            yield return Ninja.JumpBack;
            Dictionary<Patient, DataInfo> dataInfoByPatient = new Dictionary<Patient, DataInfo>();
            foreach (var site in sites)
            {
                if (!dataInfoByPatient.ContainsKey(site.Information.Patient))
                {
                    if (column is Column3DIEEG)
                    {
                        Column3DIEEG columnIEEG = (Column3DIEEG)column;
                        DataInfo dataInfo = scene.Visualization.GetDataInfo(site.Information.Patient, columnIEEG.ColumnData);
                        dataInfoByPatient.Add(site.Information.Patient, dataInfo);
                    }
                }
            }

            for (int i = 0; i < length; ++i)
            {
                Site site = sites[i];
                m_Progress = (float)i / (length - 1);
                m_Message = "Exporting " + site.Information.FullCorrectedID;
                if (!(site.State.IsExcluded || site.State.IsBlackListed || site.State.IsMasked || site.State.IsOutOfROI))
                {
                    Vector3 sitePosition = sitePositions[i];
                    string EEG = "", POS = "", mainEventName = "", code = "";
                    if (column is Column3DIEEG)
                    {
                        Column3DIEEG columnIEEG = (Column3DIEEG)column;
                        DataInfo dataInfo = dataInfoByPatient[site.Information.Patient];
                        EEG = dataInfo.EEG;
                        POS = dataInfo.POS;
                        Bloc bloc = columnIEEG.ColumnData.Bloc;
                        Data.Experience.Protocol.Event mainEvent = bloc.MainEvent;
                        mainEventName = mainEvent.Name;
                        code = String.Join(" ", mainEvent.Codes.Select(c => c.ToString()).ToArray());
                    }
                    csvBuilder.AppendLine(String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                            site.Information.ChannelName,
                            site.Information.Patient.Name,
                            site.Information.Patient.Place,
                            site.Information.Patient.Date,
                            sitePosition.x.ToString("N2"),
                            sitePosition.y.ToString("N2"),
                            sitePosition.z.ToString("N2"),
                            scene.ColumnManager.SelectedImplantation.Name,
                            EEG,
                            POS,
                            mainEventName,
                            code));
                }
            }

            using (StreamWriter sw = new StreamWriter(savePath))
            {
                sw.Write(csvBuilder.ToString());
            }

            yield return Ninja.JumpToUnity;
            m_Circle.Close();
            m_Circle = null;
            ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Informational, "Sites exported", "The sites of the selected ROI (" + column.SelectedROI.Name + ") have been sucessfully exported to " + savePath);
        }
        #endregion
    }
}