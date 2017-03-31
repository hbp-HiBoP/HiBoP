using UnityEngine;

namespace HBP.Data.Visualisation
{
    /**
    * \class Icon
    * \author Adrien Gannerie
    * \version 1.0
    * \date 10 janvier 2017
    * \brief Icon of the Iconic scenario.
    * 
    * \details Icon of the Iconic Scenario which contains:
    *   - \a Label.
    *   - \a Illustration path.
    *   - \a Start position.
    *   - \a End position.
    */
    public class Icon
    {
        /// <summary>
        /// Label.
        /// </summary>
        private string label;
        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        /// <summary>
        /// Illustration path.
        /// </summary>
        private string illustrationPath;
        public string IllustrationPath
        {
            get { return illustrationPath; }
            set { illustrationPath = value; }
        }

        /// <summary>
        /// Start position.
        /// </summary>
        private int startPosition;
        public int StartPosition
        {
            get { return startPosition; }
            set { startPosition = value; }
        }

        /// <summary>
        /// End position.
        /// </summary>
        private int endPosition;
        public int EndPosition
        {
            get { return endPosition; }
            set { endPosition = value; }
        }

        /// <summary>
        /// Create a new icon instance.
        /// </summary>
        /// <param name="icon">Icon.</param>
        /// <param name="frequency">Frequency of the data.</param>
        /// <param name="timeLine">Time line of the bloc.</param>
        public Icon(Experience.Protocol.Icon icon, float frequency,TimeLine timeLine)
        {
            Label = icon.Name;
            IllustrationPath = icon.IllustrationPath;
            StartPosition = Mathf.Clamp(Mathf.FloorToInt((icon.Window.Start) * 0.001f * frequency) + timeLine.MainEvent.Position,0, timeLine.Lenght - 1);
            EndPosition = Mathf.Clamp(Mathf.FloorToInt((icon.Window.End) * 0.001f * frequency) + timeLine.MainEvent.Position,0,timeLine.Lenght-1);
        }

        /// <summary>
        /// Create a new icon instance with default value;
        /// </summary>
        public Icon() : this(new Experience.Protocol.Icon(),0.0f,new TimeLine())
        {
        }
    }
}
