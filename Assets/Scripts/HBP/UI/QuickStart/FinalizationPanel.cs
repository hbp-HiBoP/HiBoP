using CielaSpike;
using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Visualization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
{
    public class FinalizationPanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private InputField m_ProjectName;
        [SerializeField] private FolderSelector m_ProjectLocation;
        #endregion

        #region Public Methods
        public override bool OpenNextPanel()
        {
            if (string.IsNullOrEmpty(m_ProjectName.text))
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Name field must be filled", "You need to name your project in order to continue.");
                return false;
            }
            if (string.IsNullOrEmpty(m_ProjectLocation.Folder) || !Directory.Exists(m_ProjectLocation.Folder))
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Directory does not exist", "The input directory does not exist.");
                return false;
            }
            // Add visualization
            if (ApplicationState.ProjectLoaded.Protocols.Count == 0) // Anatomical
            {
                Data.Visualization.Visualization visualization = new Data.Visualization.Visualization("QuickStart Anatomy", ApplicationState.ProjectLoaded.Patients, new Data.Visualization.Column[] { new Data.Visualization.AnatomicColumn("Anatomy", new Data.Visualization.BaseConfiguration()) });
                ApplicationState.ProjectLoaded.SetVisualizations(new Data.Visualization.Visualization[] { visualization });
            }
            else // Functional
            {
                List<Patient> patients = new List<Patient>();
                foreach (var patient in ApplicationState.ProjectLoaded.Patients)
                {
                    if (ApplicationState.ProjectLoaded.Datasets[0].Data.First(d => (d as IEEGDataInfo).Patient == patient).IsOk)
                    {
                        patients.Add(patient);
                    }
                }
                List<IEEGColumn> columns = new List<IEEGColumn>();
                Protocol protocol = ApplicationState.ProjectLoaded.Protocols[0];
                foreach (var bloc in protocol.Blocs)
                {
                    IEEGColumn column = new IEEGColumn(string.Format("Code {0}", bloc.MainSubBloc.MainEvent.Codes[0]), new BaseConfiguration(), ApplicationState.ProjectLoaded.Datasets[0], "Data", bloc, new DynamicConfiguration());
                    columns.Add(column);
                }
                Data.Visualization.Visualization visualization = new Data.Visualization.Visualization("QuickStart", patients, columns, new VisualizationConfiguration());
                ApplicationState.ProjectLoaded.SetVisualizations(new Data.Visualization.Visualization[] { visualization });
            }
            ApplicationState.ProjectLoaded.Preferences.Name = m_ProjectName.text;
            ApplicationState.ProjectLoadedLocation = m_ProjectLocation.Folder;
            return base.OpenNextPanel();
        }
        public override void Open()
        {
            base.Open();
            if (string.IsNullOrEmpty(m_ProjectName.text))
            {
                m_ProjectName.text = ApplicationState.UserPreferences.General.Project.DefaultName;
            }
            if (string.IsNullOrEmpty(m_ProjectLocation.Folder))
            {
                m_ProjectLocation.Folder = ApplicationState.UserPreferences.General.Project.DefaultLocation;
            }
        }
        #endregion
    }
}