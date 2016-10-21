using System.IO;
using System.Xml.Serialization;
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
        public int Visualisations { get; set; }
        public string Path { get; set; }

        public ProjectInfo(string path)
        {
            Name = string.Empty;
            Patients = -1;
            Groups = -1;
            Protocols = -1;
            Datasets = -1;
            Visualisations = -1;
            Path = string.Empty;
            if(Project.IsProject(path)) 
            {
                DirectoryInfo l_projectDir = new DirectoryInfo(path);

                // Path.
                Path = path;

                // Read Name.

                FileInfo l_settings = l_projectDir.GetFiles("*" + FileExtension.Settings)[0];
                Name = ProjectSettings.LoadJson(l_settings.FullName).Name;

                DirectoryInfo[] l_directories = l_projectDir.GetDirectories();
                foreach(DirectoryInfo dir in l_directories)
                {
                    if(dir.Name == "Patients")
                    {
                        Patients = dir.GetFiles("*"+FileExtension.Patient).Length;
                    }
                    else if(dir.Name == "Groups")
                    {
                        Groups = dir.GetFiles("*" + FileExtension.Group).Length;
                    }
                    else if (dir.Name == "Protocols")
                    {
                        Protocols = dir.GetFiles("*" + FileExtension.Protocol).Length;
                    }
                    else if (dir.Name == "Datasets")
                    {
                        Datasets = dir.GetFiles("*" + FileExtension.Dataset).Length;
                    }
                    else if (dir.Name == "Visualisations")
                    {
                        int l_visualisations = 0;
                        DirectoryInfo[] l_visualisationDir = dir.GetDirectories();
                        foreach(DirectoryInfo visuDir in l_visualisationDir)
                        {
                            if(visuDir.Name == "SinglePatient")
                            {
                                l_visualisations += visuDir.GetFiles("*" + FileExtension.SingleVisualisation).Length;
                            }
                            else if(visuDir.Name == "MultiPatients")
                            {
                                l_visualisations += visuDir.GetFiles("*" + FileExtension.MultiVisualisation).Length;
                            }
                        }
                        Visualisations = l_visualisations;
                    }
                }
            }
        }
    }
}