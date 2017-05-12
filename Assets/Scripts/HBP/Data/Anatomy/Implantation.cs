using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace HBP.Data.Anatomy
{
    public enum ReferenceFrameType { Patient, MNI }

    public class Implantation
    {
        #region Properties
        const string HEADER = "ptsfile";
        public const string EXTENSION = ".pts";
        public List<Electrode> Electrodes { get; set; }
        public Brain Brain { get; set; }
        #endregion

        #region Constructor
        public Implantation()
        {
            Electrodes = new List<Electrode>();
        }
        #endregion

        #region Public Methods
        public void Load(string path, ReferenceFrameType referenceFrame, bool automaticCorrection = true)
        {
            if (!string.IsNullOrEmpty(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Exists && fileInfo.Extension == EXTENSION)
                {
                    // Read The File
                    string[] lines = File.ReadAllLines(fileInfo.FullName);

                    if (lines[0] == HEADER && lines.Length > 3)
                    {
                        // Analyse the header
                        int numberOfSites; int.TryParse(lines[2], out numberOfSites);
                        if (lines.Length == numberOfSites + 3)
                        {
                            // Work on line
                            Site[] sites = new Site[numberOfSites];
                            for (int line = 0; line < numberOfSites; line++)
                            {
                                sites[line] = new Site(lines[line + 3], referenceFrame , automaticCorrection);
                            }

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
                                Site existingSite = electrode.Sites.Find(x => x.Name == site.Name);
                                if (existingSite == null)
                                {
                                    electrode.Sites.Add(site);
                                    site.Electrode = electrode;
                                }
                                else
                                {
                                    existingSite.SetPosition(site.GetPosition(referenceFrame), referenceFrame);
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("Can not read the " + referenceFrame.ToString() + " reference frame implantation because the specified file is not in the correct format.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Can not read the " + referenceFrame.ToString() + " reference frame  implantation because the specified file is not in the correct format.");
                    }
                }
                else
                {
                    Debug.LogError("Can not read the " + referenceFrame.ToString() + " reference frame  implantation because the specified file does not exist or does not have the correct extension.");
                }
            }
            else
            {
                Debug.LogError("Can not read the " + referenceFrame.ToString() + " reference frame  implantation because the specified path is null or empty.");
            }
        }
        #endregion
    }
}