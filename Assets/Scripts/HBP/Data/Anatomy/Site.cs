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
		#region Properties
        /// <summary>
        /// Name of the plot.
        /// </summary>
		public string Name { get; set; }
        /// <summary>
        /// Electrode of the plot.
        /// </summary>
        public Electrode Electrode { get; set; }

        Vector3 patientPosition;
        Vector3 MNIPosition;
        #endregion

        #region Constructor
        /// <summary>
        /// Create site.
        /// </summary>
        /// <param name="name">Name of the site</param>
        /// <param name="position">Position of the site.</param>
        /// <param name="electrode">Electrode that contains the site.</param>
        public Site(string name)
        {
            Name = name;
            patientPosition = new Vector3();
            MNIPosition = new Vector3();
        }
        /// <summary>
        /// Create site from line.
        /// </summary>
        /// <param name="line">Line in the implantation</param>
        /// <param name="nameCorrection">Automatic correction</param>
        public Site(string line, ReferenceFrameType referenceFrame, bool nameCorrection = true) : this()
        {
            string[] lineElements = line.Split(new char[] {'\t',' '}, StringSplitOptions.RemoveEmptyEntries); // Split line into elements.

            if(lineElements.Length > 4)
            {
                string name = lineElements[0]; // Get the site name.
                if (nameCorrection) name = GetCorrectName(name); // Correct the site name.

                Vector3 position;
                float.TryParse(lineElements[1], out position.x);
                float.TryParse(lineElements[2], out position.y);
                float.TryParse(lineElements[3], out position.z);

                Name = name;
                SetPosition(position,referenceFrame);
            }
        }
        /// <summary>
        /// Create site with default values.
        /// </summary>
        public Site() : this("Unknown")
		{
		}
        #endregion

        #region Public Methods
        public void SetPosition(Vector3 position,ReferenceFrameType referenceFrame)
        {
            switch (referenceFrame)
            {
                case ReferenceFrameType.Patient:
                    patientPosition = position;
                    break;
                case ReferenceFrameType.MNI:
                    MNIPosition = position;
                    break;
                default:
                    break;
            }
        }
        public Vector3 GetPosition(ReferenceFrameType referenceFrame)
        {
            switch (referenceFrame)
            {
                case ReferenceFrameType.Patient:
                    return patientPosition;
                case ReferenceFrameType.MNI:
                    return MNIPosition;
                default:
                    return new Vector3();
            }
        }
        #endregion

        #region Private Methods
        string GetCorrectName(string name)
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
            return stringBuilder.ToString();
        }
        #endregion
    }
}