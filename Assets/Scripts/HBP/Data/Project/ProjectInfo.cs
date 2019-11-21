using System.IO;
using Ionic.Zip;

namespace HBP.Data
{
    public class ProjectInfo
    {
        #region Properties
        public ProjectPreferences Settings { get; set; }
        public int Tags { get; set; }
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
            Settings = new ProjectPreferences();
            Tags = 0;
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
                        if (entry.FileName.EndsWith(BaseTag.EXTENSION))
                        {
                            Tags++;
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
                        else if (entry.FileName.EndsWith(ProjectPreferences.EXTENSION))
                        {
                            entry.Extract(ApplicationState.ProjectTMPFolder);
                            FileInfo settingsFile = new FileInfo(ApplicationState.ProjectTMPFolder + "/" + entry.FileName);
                            Settings = Tools.Unity.ClassLoaderSaver.LoadFromJson<ProjectPreferences>(settingsFile.FullName);
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