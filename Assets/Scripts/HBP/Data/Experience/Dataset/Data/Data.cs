using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    public class Data
    {
        #region Properties
        public Dictionary<Protocol.Bloc, BlocData> DataByBloc { get; set; }
        public Dictionary<string, string> UnitByChannel { get; set; }
        public Localizer.Frequency Frequency { get; set; }
        #endregion

        #region Constructors
        public Data(DataInfo dataInfo)
        {
            RawData rawData = new RawData(dataInfo);

            // Get UnitByChannel.
            UnitByChannel = rawData.UnitByChannel;

            // Get Frequency.
            Frequency = rawData.Frequency;

            // Generate DataByBloc.
            DataByBloc = new Dictionary<Protocol.Bloc, BlocData>();
            Protocol.Protocol protocol = dataInfo.Dataset?.Protocol;
            if(protocol != null)
            {
                foreach (var bloc in protocol.Blocs)
                {
                    DataByBloc.Add(bloc, new BlocData(rawData, bloc));
                }
            }
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var blocData in DataByBloc.Values)
            {
                blocData.Clear();
            }
            DataByBloc.Clear();
            DataByBloc = new Dictionary<Protocol.Bloc, BlocData>();

            UnitByChannel.Clear();
            UnitByChannel = new Dictionary<string, string>();

            Frequency = new Localizer.Frequency();
        }
        #endregion
    }
}