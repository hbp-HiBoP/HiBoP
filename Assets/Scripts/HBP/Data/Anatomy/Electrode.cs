using System;
using System.Linq;
using System.Collections.Generic;

namespace HBP.Data.Anatomy
{
    /**
    * \class Electrode
    * \author Adrien Gannerie
    * \version 1.0
    * \date 04 janvier 2017
    * \brief Intracranial electrode.
    * 
    * \details Intracranial electrode which contains:
    *     - \a Name.
    *     - \a Plots.
    */
    public class Electrode
	{
        #region Properties
        /// <summary>
        /// Name of the electrode.
        /// </summary>
		public string Name { get; set; }
        /// <summary>
        /// Sites of the electrode.
        /// </summary>
        public Site[] Sites { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Electrode instance.
        /// </summary>
        /// <param name="name">Electrode name.</param>
        /// <param name="sites">Electrode plots.</param>
        public Electrode(string name, IEnumerable<Site> sites)
        {
            Name = name;
            Sites = sites.ToArray();
        }
        #endregion

        #region Public Static Methods
        public static bool GenerateElectrodes(IEnumerable<Site> sites, out IEnumerable<Electrode> electrodes)
        {
            electrodes = new List<Electrode>();
            foreach (var site in sites)
            {

            }
        }
        /// <summary>
        /// Find electrode name from a plot name.
        /// </summary>
        /// <param name="plot">Plot name.</param>
        /// <returns>Electrode name.</returns>
        public static string FindElectrodeName(Site plot)
        {
            return FindElectrodeName(plot.Name);
        }
        public static string FindElectrodeName(string plotName)
        {
            List<string> l_char = new List<string>();
            foreach (char l_elmt in plotName)
            {
                char[] charElmt = new Char[1] { l_elmt };
                string l_elmtString = new string(charElmt);
                int i;
                if (!int.TryParse(l_elmtString, out i))
                {
                    l_char.Add(l_elmtString);
                }
            }
            return string.Concat(l_char.ToArray());
        }
        #endregion
    }
}