using System;
using System.IO;
using System.Collections.Generic;

namespace HBP.Data.Anatomy
{
    public class Implantation
    {
        #region Properties
        /** Header of a implantation file. */
        const string HEADER = "ptsfile";
        public const string EXTENSION = ".pts";

        public List<Electrode> Electrodes { get; set; }
        #endregion

        #region Constructor
        public Implantation(string path, bool automaticCorrection = true)
        {
            Electrodes = new List<Electrode>();
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Exists && fileInfo.Extension == EXTENSION)
            {
                // Read The File
                StreamReader streamReader = new StreamReader(path);
                string text = streamReader.ReadToEnd();
                string[] lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                // Analyse the header
                int plotsNumber; int.TryParse(lines[2], out plotsNumber);
                if (lines[0] == HEADER && lines.Length == plotsNumber + 3)
                {
                    // Work on line
                    Plot[] plots = new Plot[plotsNumber];
                    for (int l = 0; l < plotsNumber; l++)
                    {
                        // Chercher par ici !
                        plots[l] = new Plot(lines[l + 3], automaticCorrection);
                    }

                    // Separate plots into electrodes.
                    foreach (Plot plot in plots)
                    {
                        string electrodeName = Electrode.FindElectrodeName(plot);
                        Electrode electrode = Electrodes.Find(x => x.Name == electrodeName);
                        if (electrode == null)
                        {
                            electrode = new Electrode(electrodeName);
                            Electrodes.Add(electrode);
                        }
                        electrode.Plots.Add(plot);
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public string[] GetPlotsName()
        {
            List<string> names = new List<string>();
            foreach(Electrode electrode in Electrodes)
            {
                foreach(Plot plot in electrode.Plots)
                {
                    names.Add(plot.Name);
                }
            }
            return names.ToArray();
        }
        #endregion
    }
}