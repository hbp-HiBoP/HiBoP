﻿using UnityEngine;
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
        /// Position of the plot.
        /// </summary>
        public Vector3 Position { get; set; }
        #endregion

        #region Constructors
        public Site(string name,Vector3 position)
        {
            Name = name;
            Position = position;
        }
        public Site() : this("New site", Vector3.zero) { }
        #endregion

        #region Public static Methods
        public static bool ReadLine(string line, out Site site)
        {
            site = new Site();
            string[] elements = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries); // Split line into elements.
            float x, y, z;
            if (elements.Length >= 4 && float.TryParse(elements[1], out x) && float.TryParse(elements[2], out y) && float.TryParse(elements[3], out z))
            {
                site = new Site(elements[0], new Vector3(x, y, z));
                return true;
            }
            return false;
        }
        #endregion
    }
}