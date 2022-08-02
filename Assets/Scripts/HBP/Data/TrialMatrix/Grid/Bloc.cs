using HBP.Data.Informations;
using System.Linq;
using static HBP.Data.TrialMatrix.Grid.TrialMatrixGrid;

namespace HBP.Data.TrialMatrix.Grid
{
    public class Bloc
    {
        #region Properties
        public string Title { get; set; }
        public ChannelBloc[] ChannelBlocs { get; set; }
        public Core.Data.Bloc Data { get; set; }
        #endregion

        #region Constructors
        public Bloc(Core.Data.Bloc bloc, TrialMatrixData dataStruct, ChannelStruct[] channels)
        {
            Title = bloc.Name;
            Data = bloc;
            ChannelBlocs = channels.Select(c => new ChannelBloc(bloc, dataStruct, c)).ToArray();
        }
        #endregion
    }
}