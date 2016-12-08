namespace HBP.Data.TrialMatrix
{
    public class Bloc
    {
        #region Properties
        Experience.Protocol.Bloc m_pbloc;
        /// <summary>
        /// Bloc to display
        /// </summary>
        public Experience.Protocol.Bloc PBloc
        {
            get { return m_pbloc;  }
            set { m_pbloc = value; }
        }

        Line[] m_lines;
        /// <summary>
        /// Lines of the bloc.
        /// </summary>
        public Line[] Lines
        {
            get
            {
                return m_lines;
            }
            set
            {
                m_lines = value;
            }
        }
        #endregion

        #region Constructor
        public Bloc(Experience.Protocol.Bloc pbloc,Line[] lines)
        {
            PBloc = pbloc;
            Lines = lines;
        }

        public Bloc(Experience.Protocol.Bloc bloc, Localizer.POS pos, float[] data, float frequency)
        {
            PBloc = bloc;
            Lines = Line.MakeLines(bloc, pos, data, frequency);
        }

        public Bloc() : this(new Experience.Protocol.Bloc(),new Line[0])
        {
        }
        #endregion

        #region Public Methods
        public Line[] GetLines(int[] lines)
        {
            Line[] result = new Line[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                result[i] = m_lines[lines[i]];
            }
            return result;
        }
        #endregion
    }
}