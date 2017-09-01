using UnityEngine;
using System;
using System.Linq;

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
        public string ID
        {
            get
            {
                return Patient.ID + "_" + Name;
            }
        }
		public string Name { get; set; }
        public string Electrode
        {
            get
            {
                return Name.Where((c) => !Char.IsNumber(c)).ToString();
            }
        }
        public Patient Patient { get; set; }
        public Vector3 Position { get; set; }
        #endregion

        #region Constructors
        public Site(string name, Patient patient, Vector3 position)
        {
            Name = name;
            Patient = patient;
            Position = position;
        }
        public Site() : this("New site", null, Vector3.zero) { }
        #endregion

        #region Public static Methods
        public static bool ReadLine(string line, Patient patient, out Site site)
        {
            site = new Site();
            string[] elements = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries); // Split line into elements.
            float x, y, z;
            if (elements.Length >= 4 && float.TryParse(elements[1], out x) && float.TryParse(elements[2], out y) && float.TryParse(elements[3], out z))
            {
                site = new Site(elements[0], patient, new Vector3(x, y, z));
                return true;
            }
            return false;
        }
        #endregion
    }
}