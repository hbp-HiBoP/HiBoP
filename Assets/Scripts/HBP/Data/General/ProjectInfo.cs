using System.IO;
using HBP.Data.Preferences;
using System.Linq;
using Ionic.Zip;

namespace HBP.Data.General
{
    public class ProjectInfo
    {
        #region Properties
        public ProjectSettings Settings { get; set; }
        public int Patients { get; set; }
        public int Groups { get; set; }
        public int Protocols { get; set; }
        public int Datasets { get; set; }
        public int Visualizations { get; set; }
        public string Path { get; set; }
        #endregion

        #region Constructors
        public ProjectInfo()
        {
            Settings = new ProjectSettings();
            Patients = 0;
            Groups = 0;
            Protocols = 0;
            Datasets = 0;
            Visualizations = 0;
            Path = string.Empty;
        }
        public ProjectInfo(string path) : base()
        {
            if (Project.IsProject(path))
            {
                Path = path;
                using (ZipFile zip = ZipFile.Read(path))
                {
                    foreach (ZipEntry entry in zip)
                    {
                        if (entry.FileName.EndsWith(Patient.EXTENSION))
                        {
                            Patients++;
                        }
                        else if(entry.FileName.EndsWith(Group.EXTENSION))
                        {
                            Groups++;
                        }
                        else if (entry.FileName.EndsWith(Experience.Protocol.Protocol.EXTENSION))
                        {
                            Protocols++;
                        }
                        else if(entry.FileName.EndsWith(Experience.Dataset.Dataset.EXTENSION))
                        {
                            Datasets++;
                        }
                        else if (entry.FileName.EndsWith(Visualization.Visualization.EXTENSION))
                        {
                            Visualizations++;
                        }
                        else if (entry.FileName.EndsWith(ProjectSettings.EXTENSION))
                        {
                            entry.Extract(ApplicationState.ProjectTMPFolder);
                            FileInfo settingsFile = new FileInfo(ApplicationState.ProjectTMPFolder + "/" + entry.FileName);
                            Settings = Tools.Unity.ClassLoaderSaver.LoadFromJson<ProjectSettings>(settingsFile.FullName);
                            settingsFile.Directory.Delete(true);                        
                        }
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