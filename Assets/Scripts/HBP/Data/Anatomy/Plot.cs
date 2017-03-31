using UnityEngine;
using System;
using System.Text;

namespace HBP.Data.Anatomy
{
    /**
    * \class Plot
    * \author Adrien Gannerie
    * \version 1.0
    * \date 05 janvier 2017
    * \brief Electrode Plot.
    * 
    * \details Class which define a electrode plot  which contains:
    *   - \a Name.
    *   - \a Position.
    */
    public class Plot
	{
		#region Attributs
        /// <summary>
        /// Name of the plot.
        /// </summary>
		public string Name { get; set; }
        /// <summary>
        /// Position of the plot.
        /// </summary>
		public Vector3 Position { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a plot by a implantation line.
        /// </summary>
        /// <param name="line">Line in the implantation</param>
        /// <param name="automaticCorrection">Automatic correction</param>
        public Plot(string line, bool automaticCorrection = true)
        {
            string sep = "\t";
            string[] elements = line.Split(sep.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string name = elements[0];

            if (automaticCorrection) CorrectPlotName(ref name);

            float x, y, z;
            float.TryParse(elements[1], out x);
            float.TryParse(elements[2], out y);
            float.TryParse(elements[3], out z);
            Name = name;
            Position = new Vector3(x, y, z);
        }
        /// <summary>
        /// Create a new plot.
        /// </summary>
        /// <param name="name">Name of the plot.</param>
        /// <param name="position">Position of the plot center.</param>
        /// <param name="radius">Radius of the plot.</param>
        public Plot(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }
        /// <summary>
        /// Create a new plot with default values..
        /// </summary>
        public Plot() : this("Unknown",new Vector3(0,0,0))
		{
		}
        #endregion

        #region Private Methods
        void CorrectPlotName(ref string name)
        {
            name = name.ToUpper();
            StringBuilder stringBuilder = new StringBuilder(name);
            int pIndex = name.LastIndexOf("P");
            if (pIndex != 0 && name.Length > pIndex + 1)
            {
                char c1 = stringBuilder[pIndex + 1];
                if (char.IsNumber(c1))
                {
                    stringBuilder[pIndex] = "'".ToCharArray()[0];
                }
            }
            name = stringBuilder.ToString();
        }
        #endregion
    }
}