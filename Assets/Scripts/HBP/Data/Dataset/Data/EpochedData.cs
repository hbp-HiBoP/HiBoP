using System.Collections.Generic;

namespace HBP.Core.Data
{
    public class EpochedData : Data
    {
        #region Properties
        public virtual Dictionary<Bloc, BlocData> DataByBloc { get; set; }
        public virtual Dictionary<string, string> UnitByChannel { get; set; }
        public virtual Tools.Frequency Frequency { get; set; }
        #endregion

        #region Constructors
        public EpochedData(DataInfo dataInfo)
        {
            DynamicData rawData = new DynamicData(dataInfo);

            // Get UnitByChannel.
            UnitByChannel = rawData.UnitByChannel;

            // Get Frequency.
            Frequency = rawData.Frequency;

            // Generate DataByBloc.
            DataByBloc = new Dictionary<Bloc, BlocData>();
            Protocol protocol = dataInfo.Dataset?.Protocol;
            if (protocol != null)
            {
                foreach (var bloc in protocol.Blocs)
                {
                    DataByBloc.Add(bloc, new BlocData(rawData, bloc));
                }
            }
        }
        #endregion

        #region Public Methods
        public override void Clear()
        {
            foreach (var blocData in DataByBloc.Values) blocData.Clear();
            DataByBloc.Clear();
            UnitByChannel.Clear();
            Frequency = new Tools.Frequency();
        }
        #endregion
    }
}