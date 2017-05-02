using UnityEngine;
using System;
using System.Text;

namespace HBP.Data.Anatomy
{
    /**
    * \class Site
    * \author Adrien Gannerie
    * \version 1.0
    * \date 26 avril 2017
    * \brief Electrode site.
    * 
    * \details Class which define a electrode site  which contains:
    *   - \a Name.
    *   - \a Position.
    *   - \a Reference to his parent electrode.
    */
    public class Site
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
        /// <summary>
        /// Electrode of the plot.
        /// </summary>
        public Electrode Electrode { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create site.
        /// </summary>
        /// <param name="name">Name of the site</param>
        /// <param name="position">Position of the site.</param>
        /// <param name="electrode">Electrode that contains the site.</param>
        public Site(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }
        /// <summary>
        /// Create site from line.
        /// </summary>
        /// <param name="line">Line in the implantation</param>
        /// <param name="nameCorrection">Automatic correction</param>
        public Site(string line, bool nameCorrection = true) : this()
        {
            string[] lineElements = line.Split(new char[] {'\t',' '}, StringSplitOptions.RemoveEmptyEntries); // Split line into elements.

            if(lineElements.Length > 4)
            {
                string name = lineElements[0]; // Get the site name.
                if (nameCorrection) CorrectSiteName(ref name); // Correct the site name.

                Vector3 position;
                float.TryParse(lineElements[1], out position.x);
                float.TryParse(lineElements[2], out position.y);
                float.TryParse(lineElements[3], out position.z);

                Name = name;
                Position = position;
            }
        }
        /// <summary>
        /// Create site with default values.
        /// </summary>
        public Site() : this("Unknown",new Vector3(0,0,0))
		{
		}
        #endregion

        #region Private Methods
        void CorrectSiteName(ref string name)
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