using HBP.Core.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using HBP.Core.Database;

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
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Name field must be filled", "You need to name your project in order to continue.").Forget();
                return false;
            }

            if (string.IsNullOrEmpty(m_ProjectLocation.Folder) || !Directory.Exists(m_ProjectLocation.Folder))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Directory does not exist", "The input directory does not exist.").Forget();
                return false;
            }

            // Add visualization
            if (DatabaseManager.Database.Protocols.Count == 0) // Anatomical
            {
                Visualization visualization = new("QuickStart Anatomy", ApplicationState.LoadedProject.Patients, new Column[] { new AnatomicColumn("Anatomy", new BaseConfiguration()) });
                ApplicationState.LoadedProject.SetVisualizations(new Visualization[] { visualization });
            }
            else // Functional
            {
                List<Patient> patients = new();
                foreach (var patient in ApplicationState.LoadedProject.Patients)
                {
                    if (ApplicationState.LoadedProject.Datasets[0].Data.First(d => (d as IEEGDataInfo).Patient == patient).IsOk)
                    {
                        patients.Add(patient);
                    }
                }

                List<IEEGColumn> columns = new();
                Protocol protocol = DatabaseManager.Database.Protocols[0];
                foreach (var bloc in protocol.Blocs)
                {
                    IEEGColumn column = new(string.Format("Code {0}", bloc.MainSubBloc.MainEvent.Codes[0]), new BaseConfiguration(), ApplicationState.LoadedProject.Datasets[0], "Data", bloc, new DynamicConfiguration());
                    columns.Add(column);
                }

                Visualization visualization = new("QuickStart", patients, columns, new VisualizationConfiguration());
                ApplicationState.LoadedProject.SetVisualizations(new Visualization[] { visualization });
            }

            ApplicationState.LoadedProject.Name = m_ProjectName.text;
            ApplicationState.LoadedProjectLocation = m_ProjectLocation.Folder;
            return base.OpenNextPanel();
        }

        public override void Open()
        {
            base.Open();
            if (string.IsNullOrEmpty(m_ProjectName.text))
            {
                m_ProjectName.text = PersistentDataManager.UserPreferences.General.Project.DefaultName;
            }

            if (string.IsNullOrEmpty(m_ProjectLocation.Folder))
            {
                m_ProjectLocation.Folder = PersistentDataManager.UserPreferences.General.Project.DefaultLocation;
            }
        }

        #endregion
    }
}
