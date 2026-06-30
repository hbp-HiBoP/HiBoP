using System.IO;
using Ionic.Zip;
using HBP.Core.Exceptions;
using HBP.Core.Tools;

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
        }
        public ProjectInfo(string path) : base()
        {
            if (Project.IsProject(path))
            {
                Path = path;
                Name = System.IO.Path.GetFileNameWithoutExtension(path);
                using ZipFile zip = ZipFile.Read(path);
                foreach (ZipEntry entry in zip)
                {
                    if (entry.FileName.EndsWith(Patient.EXTENSION))
                    {
                        Patients++;
                    }
                    else if (entry.FileName.EndsWith(Group.EXTENSION))
                    {
                        Groups++;
                    }
                    else if (entry.FileName.EndsWith(Dataset.EXTENSION))
                    {
                        Datasets++;
                    }
                    else if (entry.FileName.EndsWith(Visualization.EXTENSION))
                    {
                        Visualizations++;
                    }
                    else if (entry.FileName.EndsWith(ProjectPreferences.EXTENSION))
                    {
                        FileInfo settingsFile = new(System.IO.Path.Combine(ApplicationState.TMPFolder, entry.FileName));
                        if (settingsFile.Exists) settingsFile.Delete();
                        entry.Extract(ApplicationState.TMPFolder);
                        try
                        {
                            Settings = ClassLoaderSaver.LoadFromJson<ProjectPreferences>(settingsFile.FullName);
                        }
                        catch (System.Exception e)
                        {
                            SettingsLoadException = e;
                            Settings = new ProjectPreferences();
                            Settings.CanLoadProject = false;
                        }
                        settingsFile.Directory.Delete(true);
                    }
                }
            }
            else
            {
                throw new DirectoryNotProjectException(path);
            }
        }
        #endregion
    }
}
