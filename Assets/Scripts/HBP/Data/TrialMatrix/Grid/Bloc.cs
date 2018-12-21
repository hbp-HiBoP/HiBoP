using HBP.Data.Informations;
using System.Linq;
using p = HBP.Data.Experience.Protocol;

namespace HBP.Data.TrialMatrix.Grid
{
    public class Bloc
    {
        #region Properties
        public string Title { get; set; }
        public ChannelBloc[] ChannelBlocs { get; set; }
        public p.Bloc Data { get; set; }
        #endregion

        #region Constructors
        public Bloc(p.Bloc bloc, DataStruct dataStruct, ChannelStruct[] channels)
        {
            Title = bloc.Name;
            Data = bloc;
            ChannelBlocs = channels.Select(c => new ChannelBloc(bloc, dataStruct, c)).ToArray();
        }
        #endregion
    }
}