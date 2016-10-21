using UnityEngine;

namespace HBP.Data.Patient
{
    /// <summary>
    /// Class which define a plot and contains :
    ///     - A name.
    ///     - Position of the plot.
    ///     - Radius of the plot.
    /// </summary>
	public class Plot
	{
		#region Attributs
		string m_name;
        /// <summary>
        /// Name of the plot.
        /// </summary>
		public string Name{get{return m_name;}set{m_name=value;}}

		Vector3 m_position;
        /// <summary>
        /// Position of the plot.
        /// </summary>
		public Vector3 Position{get{return m_position;}set{m_position=value;}}

		float m_radius;
        /// <summary>
        /// Radius of the plot.
        /// </summary>
		public float Radius{get{return m_radius;}set{m_radius=value;}}
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new plot.
        /// </summary>
        /// <param name="name">Name of the plot.</param>
        /// <param name="position">Position of the plot center.</param>
        /// <param name="radius">Radius of the plot.</param>
        public Plot(string name, Vector3 position, float radius)
        {
            m_name = name;
            m_position = position;
            m_radius = radius;
        }

        /// <summary>
        /// Create a new plot.
        /// </summary>
        public Plot() : this("Unknown",new Vector3(0,0,0),0)
		{
		}
		#endregion
	}
}
