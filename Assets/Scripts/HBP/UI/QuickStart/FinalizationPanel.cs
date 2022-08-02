using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.Unity;
using UnityEngine;
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
                Core.Data.Visualization visualization = new Core.Data.Visualization("QuickStart Anatomy", ApplicationState.ProjectLoaded.Patients, new Core.Data.Column[] { new Core.Data.AnatomicColumn("Anatomy", new Core.Data.BaseConfiguration()) });
                ApplicationState.ProjectLoaded.SetVisualizations(new Core.Data.Visualization[] { visualization });
            }
            else // Functional
            {
                List<Core.Data.Patient> patients = new List<Core.Data.Patient>();
                foreach (var patient in ApplicationState.ProjectLoaded.Patients)
                {
                    if (ApplicationState.ProjectLoaded.Datasets[0].Data.First(d => (d as Core.Data.IEEGDataInfo).Patient == patient).IsOk)
                    {
                        patients.Add(patient);
                    }
                }
                List<Core.Data.IEEGColumn> columns = new List<Core.Data.IEEGColumn>();
                Core.Data.Protocol protocol = ApplicationState.ProjectLoaded.Protocols[0];
                foreach (var bloc in protocol.Blocs)
                {
                    Core.Data.IEEGColumn column = new Core.Data.IEEGColumn(string.Format("Code {0}", bloc.MainSubBloc.MainEvent.Codes[0]), new Core.Data.BaseConfiguration(), ApplicationState.ProjectLoaded.Datasets[0], "Data", bloc, new Core.Data.DynamicConfiguration());
                    columns.Add(column);
                }
                Core.Data.Visualization visualization = new Core.Data.Visualization("QuickStart", patients, columns, new Core.Data.VisualizationConfiguration());
                ApplicationState.ProjectLoaded.SetVisualizations(new Core.Data.Visualization[] { visualization });
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