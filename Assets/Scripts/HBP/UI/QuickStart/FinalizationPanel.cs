using HBP.Core.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI.Main.QuickStart
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
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Name field must be filled", "You need to name your project in order to continue.");
                return false;
            }
            if (string.IsNullOrEmpty(m_ProjectLocation.Folder) || !Directory.Exists(m_ProjectLocation.Folder))
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Directory does not exist", "The input directory does not exist.");
                return false;
            }
            // Add visualization
            if (ApplicationState.ProjectLoaded.Protocols.Count == 0) // Anatomical
            {
                Visualization visualization = new Visualization("QuickStart Anatomy", ApplicationState.ProjectLoaded.Patients, new Column[] { new AnatomicColumn("Anatomy", new BaseConfiguration()) });
                ApplicationState.ProjectLoaded.SetVisualizations(new Visualization[] { visualization });
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
                Visualization visualization = new Visualization("QuickStart", patients, columns, new VisualizationConfiguration());
                ApplicationState.ProjectLoaded.SetVisualizations(new Visualization[] { visualization });
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