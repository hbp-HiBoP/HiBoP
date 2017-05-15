using System.IO;
using HBP.Data.Settings;

namespace HBP.Data.General
{
    public struct ProjectInfo
    {
        public string Name { get; set; }
        public int Patients { get; set; }
        public int Groups { get; set; }
        public int Protocols { get; set; }
        public int Datasets { get; set; }
        public int Visualizations { get; set; }
        public string Path { get; set; }

        public ProjectInfo(string path)
        {
            Name = string.Empty;
            Patients = -1;
            Groups = -1;
            Protocols = -1;
            Datasets = -1;
            Visualizations = -1;
            Path = string.Empty;
            if(Project.IsProject(path)) 
            {
                DirectoryInfo l_projectDir = new DirectoryInfo(path);

                // Path.
                Path = path;

                // Read Name.

                FileInfo l_settings = l_projectDir.GetFiles("*" + ProjectSettings.EXTENSION)[0];
                Name = Tools.Unity.ClassLoaderSaver.LoadFromJson<ProjectSettings>(l_settings.FullName).Name;

                DirectoryInfo[] l_directories = l_projectDir.GetDirectories();
                foreach(DirectoryInfo dir in l_directories)
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
                        int l_visualizations = 0;
                        DirectoryInfo[] l_visualizationDir = dir.GetDirectories();
                        foreach(DirectoryInfo visuDir in l_visualizationDir)
                        {
                            if(visuDir.Name == "SinglePatient")
                            {
                                l_visualizations += visuDir.GetFiles("*" + Visualization.SinglePatientVisualization.EXTENSION).Length;
                            }
                            else if(visuDir.Name == "MultiPatients")
                            {
                                l_visualizations += visuDir.GetFiles("*" + Visualization.MultiPatientsVisualization.EXTENSION).Length;
                            }
                        }
                        Visualizations = l_visualizations;
                    }
                }
            }
        }
    }
}