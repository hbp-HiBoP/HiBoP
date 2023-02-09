﻿using System.IO;
using Ionic.Zip;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.Core.Data
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
                        else if(entry.FileName.EndsWith(Group.EXTENSION))
                        {
                            Groups++;
                        }
                        else if (entry.FileName.EndsWith(Protocol.EXTENSION))
                        {
                            Protocols++;
                        }
                        else if(entry.FileName.EndsWith(Dataset.EXTENSION))
                        {
                            Datasets++;
                        }
                        else if (entry.FileName.EndsWith(Visualization.EXTENSION))
                        {
                            Visualizations++;
                        }
                        else if (entry.FileName.EndsWith(ProjectPreferences.EXTENSION) && entry.FileName.StartsWith(System.IO.Path.GetFileNameWithoutExtension(path)))
                        {
                            FileInfo settingsFile = new FileInfo(System.IO.Path.Combine(ApplicationState.TMPFolder, entry.FileName));
                            if (settingsFile.Exists) settingsFile.Delete();
                            entry.Extract(ApplicationState.TMPFolder);
                            try
                            {
                                Settings = ClassLoaderSaver.LoadFromJson<ProjectPreferences>(settingsFile.FullName);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogException(e);
                                Settings = new ProjectPreferences(System.IO.Path.GetFileNameWithoutExtension(path));
                                Settings.CanLoadProject = false;
                            }
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