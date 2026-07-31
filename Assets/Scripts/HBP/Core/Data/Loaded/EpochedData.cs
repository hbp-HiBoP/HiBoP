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

        public EpochedData(DataInfo dataInfo) : this(dataInfo, new DynamicData(dataInfo))
        {
        }

        internal EpochedData(DataInfo dataInfo, DynamicData rawData)
        {
            // Get UnitByChannel.
            UnitByChannel = new Dictionary<string, string>(rawData.UnitByChannel);

            // Get Frequency.
            Frequency = rawData.Frequency;

            // Generate DataByBloc.
            DataByBloc = new Dictionary<Bloc, BlocData>();
            foreach (var bloc in dataInfo.Protocol.Blocs)
            {
                DataByBloc.Add(bloc, new BlocData(rawData, bloc));
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
