using System.Collections.Generic;

namespace HBP.Data.Visualisation
{
    /// <summary>
    /// Iconic scenario.
    /// </summary>
    public class IconicScenario
    {
        #region Properties
        /// <summary>
        /// Icons of the iconic scenario
        /// </summary>
        private Icon[] m_icons;
        public Icon[] Icons { get { return m_icons; } set { m_icons = value; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bloc">Bloc</param>
        /// <param name="frequency">frequency</param>
        /// <param name="mainEventPosition">main event position</param>

        public IconicScenario(Experience.Protocol.Bloc bloc, float frequency, TimeLine timeLine)
        {
            List<Icon> l_icons = new List<Icon>();
            foreach(Experience.Protocol.Icon icon in bloc.Scenario.Icons)
            {
                l_icons.Add(new Icon(icon, frequency,timeLine));
            }
            Icons = l_icons.ToArray();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public IconicScenario() : this(new Experience.Protocol.Bloc(),0.0f,new TimeLine())
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reposition icons.
        /// </summary>
        /// <param name="diffBefore">Difference of the size before the main event.</param>
        public void Reposition(int diffBefore)
        {
            foreach(Icon icon in Icons)
            {
                icon.Min += diffBefore;
                icon.Max += diffBefore;
            }

        }
        #endregion
    }
}
