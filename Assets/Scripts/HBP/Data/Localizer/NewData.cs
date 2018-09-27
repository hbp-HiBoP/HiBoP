using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Data.Localizer
{
    public class NewData
    {
        #region Properties
        public Trial[] Trials { get; set; }
        #endregion

        #region Constructor
        public NewData(float[] values, POS pos, float frequency, Experience.Protocol.Bloc bloc)
        {
            Dictionary<Experience.Protocol.Event, int[]> positionByEvent = bloc.SubBlocs.SelectMany((s) => s.Events).ToDictionary((e) => e, (e) => pos.GetIndexes(e.Codes).ToArray());
            int[] MainSubBlocMainEventPositions = positionByEvent[bloc.MainSubBloc.MainEvent];
            List<Trial> trials = new List<Trial>(MainSubBlocMainEventPositions.Length);

            Tuple<int, int> researchZone;
            // All main event position but the last one.
            for (int i = 0; i < MainSubBlocMainEventPositions.Length - 1; i++)
            {
                researchZone = new Tuple<int, int>(MainSubBlocMainEventPositions[i], MainSubBlocMainEventPositions[i + 1]);
                trials.Add(new Trial(values, researchZone, positionByEvent, bloc, frequency));
            }

            // The last main event position
            researchZone = new Tuple<int, int>(MainSubBlocMainEventPositions[MainSubBlocMainEventPositions.Length - 1], int.MaxValue);
            trials.Add(new Trial(values, researchZone, positionByEvent, bloc, frequency));
        }
        #endregion
    }
}