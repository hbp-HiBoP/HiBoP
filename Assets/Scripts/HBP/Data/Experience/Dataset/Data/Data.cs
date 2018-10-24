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
            Dataset dataset = ApplicationState.ProjectLoaded.Datasets.FirstOrDefault(d => d.Data.Contains(dataInfo));
            Protocol.Protocol protocol = dataset != null ? dataset.Protocol : null;
            if(protocol != null)
            {
                foreach (var bloc in protocol.Blocs)
                {
                    DataByBloc.Add(bloc, new BlocData(rawData, bloc));
                }
            }
        }
        #endregion
    }
}