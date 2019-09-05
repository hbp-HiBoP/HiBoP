using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Data.Visualization
{
    /**
    * \class IconicScenario
    * \author Adrien Gannerie
    * \version 1.0
    * \date 11 janvier 2017
    * \brief Iconic scenario represent a scenario with some icons which represent a step in a experiment.
    * 
    * \details Iconic scenario represent a scenario with some icons which represent a step in a experiment
    */
    public class IconicScenario
    {
        #region Properties
        private List<Icon> m_Icons;
        /// <summary>
        /// Icons of the iconic scenario.
        /// </summary>
        public ReadOnlyCollection<Icon> Icons
        {
            get { return new ReadOnlyCollection<Icon>(m_Icons); }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new iconic scenario instance.
        /// </summary>
        /// <param name="bloc">Bloc of the iconic scenario.</param>
        /// <param name="frequency">Frequency used.</param>
        /// <param name="timeLine">TimeLine of the iconic scenario.</param>
        public IconicScenario(Experience.Protocol.Bloc bloc, Tools.CSharp.EEG.Frequency frequency, Timeline timeline)
        {
            m_Icons = new List<Icon>();
            foreach (var subBloc in bloc.SubBlocs)
            {
                SubTimeline subTimeline = timeline.SubTimelinesBySubBloc[subBloc];
                foreach (var subBlocIcon in subBloc.Icons)
                {
                    Icon icon = new Icon(subBlocIcon, frequency, subTimeline.StatisticsByEvent[subBloc.MainEvent].RoundedIndexFromStart + subTimeline.GlobalMinIndex, timeline.Length);
                    m_Icons.Add(icon);
                }
            }
        }
        /// <summary>
        /// Create a new iconic scenario instance with default values.
        /// </summary>
        public IconicScenario() : this(new Experience.Protocol.Bloc(), new Tools.CSharp.EEG.Frequency(0) ,null)
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add icon.
        /// </summary>
        /// <param name="icon">Icon to add.</param>
        public void AddIcon(Icon icon)
        {
            m_Icons.Add(icon);
        }
        /// <summary>
        /// Add icons.
        /// </summary>
        /// <param name="icons">Icons to add.</param>
        public void AddIcon(Icon[] icons)
        {
            foreach (Icon icon in icons) AddIcon(icon);
        }
        /// <summary>
        /// Remove icon.
        /// </summary>
        /// <param name="icon">Icon to remove.</param>
        public void RemoveIcon(Icon icon)
        {
            m_Icons.Remove(icon);
        }
        /// <summary>
        /// Remove icons.
        /// </summary>
        /// <param name="icons">Icons to remove.</param>
        public void RemoveIcon(Icon[] icons)
        {
            foreach (Icon icon in icons) RemoveIcon(icon);
        }
        /// <summary>
        /// Set icons.
        /// </summary>
        /// <param name="icons">Icons to set.</param>
        public void SetIcon(Icon[] icons)
        {
            this.m_Icons = new List<Icon>();
            AddIcon(icons);
        }
        /// <summary>
        /// Reposition icons.
        /// </summary>
        /// <param name="offset">Difference of the size before the main event.</param>
        public void Reposition(int offset)
        {
            foreach(Icon icon in Icons)
            {
                icon.StartPosition += offset;
                icon.EndPosition += offset;
            }
        }
        /// <summary>
        /// Load the textures of the icons
        /// </summary>
        public void LoadIcons()
        {
            foreach (Icon icon in m_Icons)
            {
                icon.Load();
            }
        }
        #endregion
    }
}
