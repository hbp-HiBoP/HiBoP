using System;
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
        /** Electrode name. */
		public string Name { get; set; }
        /** Electrode plots. */
        public List<Site> Sites { get; set; }
        /// <summary>
        /// Implantation which contains the electrode.
        /// </summary>
        public Implantation Implantation { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Electrode instance.
        /// </summary>
        /// <param name="name">Electrode name.</param>
        /// <param name="plots">Electrode plots.</param>
        public Electrode(string name, IEnumerable<Site> plots)
        {
            Name = name;
            Sites = new List<Site>(plots);
        }
        /// <summary>
        /// Create a new Electrode instance with no plots.
        /// </summary>
        /// <param name="name">Electrode name.</param>
		public Electrode(string name) : this(name,new Site[0])
		{
		}
        /// <summary>
        /// Create a new Electrode instance with a empty name and no plots.
        /// </summary>
        public Electrode() : this(string.Empty,new Site[0])
        {
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Find electrode name from a plot name.
        /// </summary>
        /// <param name="plot">Plot name.</param>
        /// <returns>Electrode name.</returns>
        public static string FindElectrodeName(Site plot)
        {
            List<string> l_char = new List<string>();
            foreach (char l_elmt in plot.Name)
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