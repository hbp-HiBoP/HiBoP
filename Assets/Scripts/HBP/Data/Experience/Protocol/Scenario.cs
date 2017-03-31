using System;
using System.Collections.Generic;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Scenario
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Iconic scenario.
    * 
    * \details Class which define a iconic scenario and contains :
    *     - Icons.
    */
    public class Scenario : ICloneable
    {
        #region Properties
        /// <summary>
        /// Icons of the inconic Scenario.
        /// </summary>
        public List<Icon> Icons { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new scenario instance.
        /// </summary>
        /// <param name="icons">Icons of the iconic scenario.</param>
        public Scenario(List<Icon> icons)
        {
            Icons = icons;
        }
        /// <summary>
        /// Create a new scenario instance.
        /// </summary>
        public Scenario() : this(new List<Icon>())
        {
        }
        #endregion

        #region Operator
        /// <summary>
        /// Clone this instance to a new instance.
        /// </summary>
        /// <returns>Instance clone.</returns>
        public object Clone()
        {
            List<Icon> l_list = new List<Icon>(Icons.Count);
            for (int i = 0; i < Icons.Count; i++)
            {
                l_list.Add(Icons[i].Clone() as Icon);
            }
            return new Scenario(l_list);
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Scenario p = obj as Scenario;
            if (p == null)
            {
                return false;
            }
            else
            {
                return System.Linq.Enumerable.SequenceEqual(Icons,p.Icons);
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">Scenario to compare.</param>
        /// <param name="b">Scenario to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Scenario a, Scenario b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Operator not equals.
        /// </summary>
        /// <param name="a">Scenario to compare.</param>
        /// <param name="b">Scenario to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Scenario a, Scenario b)
        {
            return !(a == b);
        }
        #endregion
    }
}
