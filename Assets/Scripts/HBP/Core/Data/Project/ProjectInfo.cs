using HBP.Core.Exceptions;

namespace HBP.Core.Data
{
    public class ProjectInfo
    {
        #region Properties

        public string Name { get; set; }
        public ProjectPreferences Settings { get; set; }
        public int Patients { get; set; }
        public int Groups { get; set; }
        public int Datasets { get; set; }
        public int Visualizations { get; set; }
        public string Path { get; set; }
        public System.Exception SettingsLoadException { get; private set; }
        public ProjectManifest Manifest { get; private set; }

        #endregion

        #region Constructors

        public ProjectInfo()
        {
            Name = string.Empty;
            Settings = new ProjectPreferences();
            Patients = 0;
            Groups = 0;
            Datasets = 0;
            Visualizations = 0;
            Path = string.Empty;
            SettingsLoadException = null;
            Manifest = null;
        }

        public ProjectInfo(string path) : this()
        {
            try
            {
                ApplyManifest(ProjectManifest.Read(path, true));
            }
            catch (DirectoryNotProjectException)
            {
                throw;
            }
            catch (System.Exception exception)
            {
                throw new DirectoryNotProjectException(path, exception);
            }
        }

        #endregion

        internal ProjectManifest GetCurrentManifest()
        {
            if (Manifest == null || !Manifest.IsCurrent())
            {
                ApplyManifest(ProjectManifest.Read(Path, true));
            }

            return Manifest;
        }

        private void ApplyManifest(ProjectManifest manifest)
        {
            Manifest = manifest;
            Path = manifest.Path;
            Name = manifest.Name;
            Settings = manifest.Preferences;
            Patients = manifest.Patients;
            Groups = manifest.Groups;
            Datasets = manifest.Datasets;
            Visualizations = manifest.Visualizations;
            SettingsLoadException = manifest.PreferencesLoadException;
        }
    }
}
