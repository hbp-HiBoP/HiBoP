using System;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Informations.TrialMatrix.Grid
{
    public class TrialMatrixGrid
    {
        #region Properties
        public ChannelStruct[] Channels { get; private set; }
        public TrialMatrixData[] DataStructs { get; private set; }
        public Data[] Data { get; private set; }
        #endregion

        #region Constructors
        public TrialMatrixGrid(ChannelStruct[] channels, TrialMatrixData[] dataStruct)
        {
            Channels = channels;
            DataStructs = dataStruct;
            Data = dataStruct.Select(data => new Data(data, channels)).ToArray();
        }
        #endregion

        #region Inner Classes
        public class TrialMatrixData : IEquatable<TrialMatrixData>
        {
            public Core.Data.Dataset Dataset;
            public string Name;
            public List<Core.Data.Bloc> Blocs;

            public TrialMatrixData(Core.Data.Dataset dataset, string dataName, List<Core.Data.Bloc> blocs)
            {
                Dataset = dataset;
                Name = dataName;
                Blocs = blocs;
            }

            #region Public Methods
            public override bool Equals(object obj)
            {
                return Equals(obj as TrialMatrixData);
            }
            public bool Equals(TrialMatrixData other)
            {
                return other != null &&
                       Name == other.Name &&
                       EqualityComparer<Core.Data.Dataset>.Default.Equals(Dataset, other.Dataset);
            }
            public override int GetHashCode()
            {
                var hashCode = 252110562;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<Core.Data.Dataset>.Default.GetHashCode(Dataset);
                return hashCode;
            }
            public static bool operator ==(TrialMatrixData struct1, TrialMatrixData struct2)
            {
                return EqualityComparer<TrialMatrixData>.Default.Equals(struct1, struct2);
            }
            public static bool operator !=(TrialMatrixData struct1, TrialMatrixData struct2)
            {
                return !(struct1 == struct2);
            }
            #endregion
        }
        public class CCEPTrialMatrixData : TrialMatrixData
        {
            public ChannelStruct Source;

            public CCEPTrialMatrixData(Core.Data.Dataset dataset, string dataName, List<Core.Data.Bloc> blocs, ChannelStruct source) : base(dataset, dataName, blocs)
            {
                Source = source;
            }
        }
        public class IEEGTrialMatrixData : TrialMatrixData
        {
            public IEEGTrialMatrixData(Core.Data.Dataset dataset, string dataName, List<Core.Data.Bloc> blocs) : base(dataset, dataName, blocs)
            {

            }
        }
        #endregion
    }
}