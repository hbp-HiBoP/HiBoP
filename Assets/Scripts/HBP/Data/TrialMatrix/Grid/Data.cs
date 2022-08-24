using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;

namespace HBP.Data.Informations.TrialMatrix.Grid
{
    public class Data
    {
        #region Properties
        public string Title { get; set; }
        public Bloc[] Blocs { get; set; }
        public Vector2 Limits { get; set; }
        public Tuple<Tuple<Core.Data.Bloc, Core.Data.SubBloc>[], Core.Tools.TimeWindow>[] SubBlocsAndWindowByColumn { get; }
        public TrialMatrixGrid.TrialMatrixData DataStruct { get; set; }
        public ChannelStruct[] ChannelStructs { get; set; }
        #endregion

        #region Constructors
        public Data(TrialMatrixGrid.TrialMatrixData dataStruct, ChannelStruct[] channelStructs)
        {
            Title = dataStruct.Dataset.Name + " " + dataStruct.Name;
            Blocs = dataStruct.Blocs.Select(bloc => new Bloc(bloc, dataStruct, channelStructs)).ToArray();

            Limits = CalculateLimits(Blocs);
            SubBlocsAndWindowByColumn = Core.Data.Bloc.GetSubBlocsAndWindowByColumn(dataStruct.Blocs);
            foreach (var bloc in Blocs)
            {
                foreach (var channelBloc in bloc.ChannelBlocs)
                {
                    channelBloc.Standardize(SubBlocsAndWindowByColumn);
                }
            }

            DataStruct = dataStruct;
            ChannelStructs = channelStructs;
        }
        #endregion

        #region Private Methods
        Vector2 CalculateLimits(IEnumerable<Bloc> blocs)
        {
            List<float> values = new List<float>();
            foreach (var bloc in blocs)
            {
                foreach(var channelBloc in bloc.ChannelBlocs)
                {
                    if(channelBloc.IsFound)
                    {
                        foreach (var subBloc in channelBloc.SubBlocs)
                        {
                            foreach (var subTrial in subBloc.SubTrials)
                            {
                                values.AddRange(subTrial.Data.Values);
                            }
                        }
                    }
                }
            }
            return values.ToArray().CalculateValueLimit();
        }
        #endregion
    }
}