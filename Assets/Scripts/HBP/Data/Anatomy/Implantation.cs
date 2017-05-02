using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace HBP.Data.Anatomy
{
    public class Implantation
    {
        #region Properties
        const string HEADER = "ptsfile";
        public const string EXTENSION = ".pts";

        public enum ReferenceFrameType { Patient, MNI}
        public ReferenceFrameType ReferenceFrame { get; set; }

        public Brain Brain { get; set; }
        public List<Electrode> Electrodes { get; set; }
        #endregion

        #region Constructor
        public Implantation(string path, bool automaticCorrection = true)
        {
            if(!string.IsNullOrEmpty(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Exists && fileInfo.Extension == EXTENSION)
                {
                    // Read The File
                    string[] lines = File.ReadAllLines(fileInfo.FullName);

                    if(lines[0] == HEADER && lines.Length > 3)
                    {
                        // Analyse the header
                        int numberOfSites; int.TryParse(lines[2], out numberOfSites);
                        if (lines.Length == numberOfSites + 3)
                        {
                            // Work on line
                            Site[] sites = new Site[numberOfSites];
                            for (int line = 0; line < numberOfSites; line++)
                            {
                                sites[line] = new Site(lines[line + 3], automaticCorrection);
                            }

                            Electrodes = new List<Electrode>();
                            // Separate sites into electrodes.
                            foreach (Site site in sites)
                            {
                                string electrodeName = Electrode.FindElectrodeName(site);
                                Electrode electrode = Electrodes.Find(x => x.Name == electrodeName);
                                if (electrode == null)
                                {
                                    electrode = new Electrode(electrodeName);
                                    electrode.Implantation = this;
                                    Electrodes.Add(electrode);
                                }
                                electrode.Sites.Add(site);
                                site.Electrode = electrode;
                            }
                        }
                        else
                        {
                            Debug.LogError("Can not read the implantation because the specified file is not in the correct format.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Can not read the implantation because the specified file is not in the correct format.");
                    }
                }
                else
                {
                    Debug.LogError("Can not read the implantation because the specified file does not exist or does not have the correct extension.");
                }
            }
            else
            {
                Debug.LogError("Can not read the implantation because the specified path is null or empty.");
            }
        }
        #endregion
    }
}