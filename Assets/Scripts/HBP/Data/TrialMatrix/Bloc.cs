using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.TrialMatrix
{
    public class Bloc
    {
        #region Properties
        /// <summary>
        /// Bloc to display
        /// </summary>
        public Experience.Protocol.Bloc ProtocolBloc { get; set; }

        /// <summary>
        /// Lines of the bloc.
        /// </summary>
        public Line[] Trials { get; set; }

        public int SpacesBefore { get; set; }
        public int SpacesAfter { get; set; }
        #endregion

        #region Constructor
        public Bloc(Experience.Protocol.Bloc protocolBloc,Line[] lines)
        {
            ProtocolBloc = protocolBloc;
            Trials = lines;
        }
        public Bloc(Experience.Protocol.Bloc protocolBloc, IEnumerable<Localizer.Bloc> blocs, Module3D.Site site)
        {
            ProtocolBloc = protocolBloc;
            Trials = Line.MakeLines(protocolBloc, blocs, site).ToArray();
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
                result[i] = Trials[lines[i]];
            }
            return result;
        }
        #endregion
    }
}