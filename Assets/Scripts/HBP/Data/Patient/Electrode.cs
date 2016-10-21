using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace HBP.Data.Patient
{
    /// <summary>
    /// Class which define a electrode which contains :
    ///     - A electode name.
    ///     - Plots.
    /// </summary>
	public class Electrode
	{
		#region Parameters
		string m_name;
        /// <summary>
        /// Name of the electrode.
        /// </summary>
		public string Name{get{return m_name;}set{m_name=value;}}

        List<Plot> m_plots;
        /// <summary>
        /// Plots of the electrode.
        /// </summary>
		public List<Plot> Plots{get{return m_plots; }set{ m_plots = value;}}

        /// <summary>
        /// Header of the pts file.
        /// </summary>
		static string m_headerString = "ptsfile";
        #endregion

        #region Constructor
        /// <summary>
        /// Create new Electrode.
        /// </summary>
        /// <param name="name">Name of the electrode.</param>
        /// <param name="plots">Plots of the electrode.</param>
        public Electrode(string name, List<Plot> plots)
        {
            Name = name;
            Plots = plots;
        }
        /// <summary>
        /// Create new Electrode.
        /// </summary>
        /// <param name="name">Name of the electrode.</param>
        /// <param name="plots">Plots of the electrode.</param>
        public Electrode(string name, Plot[] plots) : this(name,new List<Plot>(plots))
        {
        }

        /// <summary>
        /// Create new Electrode.
        /// </summary>
        /// <param name="name"></param>
		public Electrode(string name) : this(name,new List<Plot>())
		{
		}
		#endregion

		#region Public Static Methods
        /// <summary>
        /// Read the implantation file.
        /// </summary>
        /// <param name="path">Path of the file to read.</param>
        /// <returns></returns>
		public static Electrode[] readImplantationFile(string path)
		{
            FileInfo l_implantation = new FileInfo(path);
            if(l_implantation.Exists && l_implantation.Extension == Settings.FileExtension.Implantation)
            {
                try
                {
                    // Read The File
                    StreamReader l_file = new StreamReader(path);
                    string l_fileString = l_file.ReadToEnd();

                    // Split the string into different Lines
                    //string[] l_lines = l_fileString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    string[] l_lines = l_fileString.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    //Debug.Log(l_lines.Length);
                    //Debug.Log(l_lines[0]);
                    // Analyse the header
                    int l_numberOfPlots; int.TryParse(l_lines[2], out l_numberOfPlots);
                    if (l_lines[0] != m_headerString)
                    {
                        Debug.LogError("Error : It's not a ptsfile");
                        return new Electrode[0];
                    }
                    else if (l_lines.Length != l_numberOfPlots + 3)
                    {
                        Debug.LogError("Error : Can't read the file");
                        return new Electrode[0];
                    }

                    // If is OK we can work on the file
                    else
                    {
                        // Work on line
                        List<string> l_linesList = new List<string>(l_lines);
                        l_linesList.RemoveRange(0, 3);
                        l_lines = l_linesList.ToArray();

                        // Electodes list
                        List<Electrode> l_electrodes = new List<Electrode>();
                        List<string> l_electrodesName = new List<string>();

                        // Read EachLine
                        foreach (string l_line in l_lines)
                        {
                            // Chercher par ici !
                            Plot l_plot = ReadLine(l_line);
                            string l_electrodeName = FindElectrodeName(l_plot.Name);
                            if (!l_electrodesName.Contains(l_electrodeName))
                            {
                                l_electrodesName.Add(l_electrodeName);
                                Electrode l_electrode = new Electrode(l_electrodeName);
                                l_electrode.Plots.Add(l_plot);
                                l_electrodes.Add(l_electrode);
                            }
                            else
                            {
                                foreach (Electrode l_electrode in l_electrodes)
                                {
                                    if (l_electrode.Name == l_electrodeName)
                                    {
                                        l_electrode.Plots.Add(l_plot);
                                    }
                                }
                            }
                        }
                        return l_electrodes.ToArray();
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    return new Electrode[0];
                }
            }
            else
            {
                return new Electrode[0];
            }

		}

        public static PlotID[] Read(Patient[] patients,bool MNI)
        {
            List<PlotID> l_plotID = new List<PlotID>();
            foreach(Patient patient in patients)
            {
                string[] l_plotName;
                if(MNI)
                {
                    l_plotName = readImplantation(patient.Brain.MNIBasedImplantation);
                }
                else
                {
                    l_plotName = readImplantation(patient.Brain.PatientBasedImplantation);
                }
                foreach(string s in l_plotName)
                {
                    l_plotID.Add(new PlotID(s, patient));
                }
            }
            return l_plotID.ToArray();
        }

        public static string[] readImplantation(string path)
        {
            FileInfo l_implantation = new FileInfo(path);
            if (l_implantation.Exists && l_implantation.Extension == Settings.FileExtension.Implantation)
            {
                try
                {
                    // Read The File
                    StreamReader l_file = new StreamReader(path);
                    string l_fileString = l_file.ReadToEnd();

                    // Split the string into different Lines
                    //string[] l_lines = l_fileString.Split(new string[] { "\r\n|\r|\n" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] l_lines = l_fileString.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    // Analyse the header
                    int l_numberOfPlots; int.TryParse(l_lines[2], out l_numberOfPlots);
                    if (l_lines[0] != m_headerString)
                    {
                        Debug.LogError("Error : It's not a ptsfile");
                        return new string[0];
                    }
                    else if (l_lines.Length != l_numberOfPlots + 3)
                    {
                        Debug.LogError("Error : Can't read the file");
                        return new string[0];
                    }

                    // If is OK we can work on the file
                    else
                    {
                        // Work on line
                        List<string> l_linesList = new List<string>(l_lines);
                        l_linesList.RemoveRange(0, 3);
                        l_lines = l_linesList.ToArray();

                        // Read EachLine
                        List<string> l_plotName = new List<string>();
                        foreach (string l_line in l_lines)
                        {
                            // Chercher par ici !
                            l_plotName.Add(ReadLine(l_line).Name);
                        }
                        return l_plotName.ToArray();
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    return new string[0];
                }
            }
            else
            {
                return new string[0];
            }

        }

        public static string[][] readImplantation(string[] path)
        {
            int imax = path.Length;
            string[][] l_plotName = new string[imax][];
            for (int i = 0; i < imax; i++)
            {
                l_plotName[i] = readImplantation(path[i]);
            }
            return l_plotName;
        }
        #endregion

        #region Private Static Methods
        /// <summary>
        /// Read a line of a pts line and return a plot.
        /// </summary>
        /// <param name="line">Line to read.</param>
        /// <returns>Plot of this line.</returns>
        static Plot ReadLine(string line)
		{
            string sep = "\t";
			string[] l_elements = line.Split(sep.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			string l_name = l_elements[0];

            if(ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType == Settings.GeneralSettings.PlotNameCorrectionTypeEnum.Active)
            {
                l_name = l_name.ToUpper();
                System.Text.StringBuilder l_nameB = new System.Text.StringBuilder(l_name);
                int pIndex = l_name.LastIndexOf("P");
                if (pIndex != 0 && l_name.Length > pIndex + 1)
                {
                    char c1 = l_nameB[pIndex + 1];
                    if (char.IsNumber(c1))
                    {
                        l_nameB[pIndex] = "'".ToCharArray()[0];
                    }
                }
                l_name = l_nameB.ToString();
            }

            float l_x,l_y,l_z;
			float.TryParse(l_elements[1],out l_x);
			float.TryParse(l_elements[2],out l_y);
			float.TryParse(l_elements[3],out l_z);
			return new Plot(l_name,new Vector3(l_x,l_y,l_z),1.0f);
		}

        /// <summary>
        /// Find electrode name.
        /// </summary>
        /// <param name="plotName">Plot name of the electrode to find.</param>
        /// <returns></returns>
		static string FindElectrodeName(string plotName)
		{
			List<string> l_char = new List<string>();
			foreach(char l_elmt in plotName)
			{
				char[] charElmt = new Char[1]{l_elmt};
				string l_elmtString = new string(charElmt);
				int i;
				if(!int.TryParse(l_elmtString,out i))
				{
					l_char.Add(l_elmtString);
				}
			}
			return string.Concat(l_char.ToArray());
		}
		#endregion
	}
}
