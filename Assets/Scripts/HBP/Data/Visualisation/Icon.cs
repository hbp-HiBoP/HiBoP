using UnityEngine;

namespace HBP.Data.Visualisation
{
    /// <summary>
    /// Icon.
    /// </summary>
    public class Icon
    {
        /// <summary>
        /// Label.
        /// </summary>
        private string m_label;
        public string Label
        {
            get { return m_label; }
            set { m_label = value; }
        }

        /// <summary>
        /// Illustration path.
        /// </summary>
        private string m_path;
        public string Path
        {
            get { return m_path; }
            set { m_path = value; }
        }

        /// <summary>
        /// Position limit min.
        /// </summary>
        private int m_min;
        public int Min
        {
            get { return m_min; }
            set { m_min = value; }
        }

        /// <summary>
        /// Position limit max.
        /// </summary>
        private int m_max;
        public int Max
        {
            get { return m_max; }
            set { m_max = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="icon">Icon.</param>
        /// <param name="frequency">Frequency of the data.</param>
        /// <param name="timeLine">Time line of the bloc.</param>

        public Icon(Experience.Protocol.Icon icon, float frequency,TimeLine timeLine)
        {
            Label = icon.Name;
            Path = icon.Image;
            float l_min = icon.Window.x * 0.001f * frequency + timeLine.MainEvent.Position;
            float l_max = icon.Window.y * 0.001f * frequency + timeLine.MainEvent.Position;
            Max = Mathf.FloorToInt((icon.Window.y) * 0.001f * frequency) + timeLine.MainEvent.Position;
            Min = Mathf.FloorToInt((icon.Window.x) * 0.001f * frequency) + timeLine.MainEvent.Position;
            float l_step = timeLine.Step;
            float l_minRealTime = timeLine.Min.Value + l_step * l_min;
            float l_maxRealTime = timeLine.Min.Value + l_step * l_max;
            //Debug.Log(l_minRealTime);
            //Debug.Log(l_maxRealTime);
            if(Min < 0)
            {
                Min = 0;
            }
            if(Max > timeLine.Size-1)
            {
                Max = timeLine.Size - 1;
            }
            //Debug.Log(Label + " " + Min + " / " + Max + "   ||   " + l_min + " / " + l_max);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Icon() : this(new Experience.Protocol.Icon(),0.0f,new TimeLine())
        {
        }
    }
}
