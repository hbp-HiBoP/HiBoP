using System.IO;
using HBP.Data.Preferences;
using System.Linq;

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
            if(Project.IsProject(path)) 
            {
                DirectoryInfo directory = new DirectoryInfo(path);

                FileInfo settingsFile = directory.GetFiles("*" + ProjectSettings.EXTENSION).First();
                Settings = Tools.Unity.ClassLoaderSaver.LoadFromJson<ProjectSettings>(settingsFile.FullName);
                Path = path;

                DirectoryInfo[] directories = directory.GetDirectories();
                foreach(DirectoryInfo dir in directories)
                {
                    if(dir.Name == "Patients")
                    {
                        Patients = dir.GetFiles("*"+Patient.EXTENSION).Length;
                    }
                    else if(dir.Name == "Groups")
                    {
                        Groups = dir.GetFiles("*" + Group.EXTENSION).Length;
                    }
                    else if (dir.Name == "Protocols")
                    {
                        Protocols = dir.GetFiles("*" + Experience.Protocol.Protocol.EXTENSION).Length;
                    }
                    else if (dir.Name == "Datasets")
                    {
                        Datasets = dir.GetFiles("*" + Experience.Dataset.Dataset.EXTENSION).Length;
                    }
                    else if (dir.Name == "Visualizations")
                    {
                        Visualizations  = dir.GetFiles("*" + Visualization.Visualization.EXTENSION).Length;
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