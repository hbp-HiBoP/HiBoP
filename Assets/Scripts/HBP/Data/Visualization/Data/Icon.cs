using UnityEngine;
using Tools.Unity;

namespace HBP.Data.Visualization
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

        private Sprite m_Illustration;
        /// <summary>
        /// Texture of the icon
        /// </summary>
        public Sprite Illustration
        {
            get
            {
                return m_Illustration;
            }
        }

        public bool Usable
        {
            get
            {
                return SpriteExtension.IsFileLoadable(illustrationPath);
            }
        }

        /// <summary>
        /// Create a new icon instance.
        /// </summary>
        /// <param name="icon">Icon.</param>
        /// <param name="frequency">Frequency of the data.</param>
        /// <param name="timeLine">Time line of the bloc.</param>
        public Icon(Experience.Protocol.Icon icon, Tools.CSharp.EEG.Frequency frequency, int mainEventPosition, int timelineLength)
        {
            Label = icon.Name;
            IllustrationPath = icon.IllustrationPath;
            StartPosition = Mathf.Clamp(frequency.ConvertToCeiledNumberOfSamples(icon.Window.Start) + mainEventPosition, 0, timelineLength - 1);
            EndPosition = Mathf.Clamp(frequency.ConvertToFlooredNumberOfSamples(icon.Window.End) + mainEventPosition, 0, timelineLength - 1);
        }

        /// <summary>
        /// Create a new icon instance with default value;
        /// </summary>
        public Icon() : this(new Experience.Protocol.Icon(), new Tools.CSharp.EEG.Frequency(0), 0, 0)
        {
        }

        /// <summary>
        /// Load an icon
        /// </summary>
        public void Load()
        {
            Sprite sprite;
            if (SpriteExtension.LoadSpriteFromFile(out sprite, IllustrationPath)) m_Illustration = sprite;
        }
    }
}
