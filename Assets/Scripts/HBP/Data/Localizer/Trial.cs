using System;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Protocol;

namespace HBP.Data.Localizer
{
    public struct Trial
    {
        #region Properties
        public Dictionary<Experience.Protocol.SubBloc, SubTrial> SubTrialBySubBloc { get; set; }
        #endregion

        #region Constructor
        public Trial(Dictionary<Experience.Protocol.SubBloc, SubTrial> subTrialBySubBloc)
        {
            SubTrialBySubBloc = subTrialBySubBloc;
        }
        public Trial(float[] values, Tuple<int,int> researchZone, Dictionary<Experience.Protocol.Event, int[]> positionByEvent, Experience.Protocol.Bloc bloc, float frequency)
        {
            SubTrialBySubBloc = bloc.SubBlocs.ToDictionary((s) => s, (s) => new SubTrial(values, researchZone, positionByEvent, s, frequency));
        }
        #endregion
    }

    public struct SubTrial
    {
        #region Properties
        public bool Found { get; set; }
        public Dictionary<Event, EventPosition> IndexByEvents { get; set; }
        public float[] Values { get; set;}
        #endregion

        #region Constructors
        public SubTrial(float[] values, Tuple<int, int> researchZone, Dictionary<Event, int[]> positionByEvent, Experience.Protocol.SubBloc subBloc, float frequency) : this(false, new Dictionary<Event, EventPosition>(), new float[0])
        {
            //foreach (var e in subBloc.Events)
            //{
            //    positionByEvent[e].FirstOrDefault()
            //}
        }
        public SubTrial(bool found, Dictionary<Event, EventPosition> indexByEvents, float[] values) : this()
        {
            Found = found;
            IndexByEvents = indexByEvents;
            Values = values;
        }
        #endregion

        #region Private Methods
        Tuple<int, int> GetNumberOfSamples(Tools.CSharp.Window window, int frequency)
        {
            return new Tuple<int, int>(UnityEngine.Mathf.CeilToInt((window.Start) * 0.001f * frequency), UnityEngine.Mathf.FloorToInt((window.End) * 0.001f * frequency));
        }
        #endregion
    }

    public struct EventPosition
    {
        /// <summary>
        /// Was the event found in this essay?
        /// </summary>
        public bool Found { get; set; }
        /// <summary>
        /// Relative index from this subBloc.
        /// </summary>
        public int RelativeIndex { get; set; }
        /// <summary>
        /// Global index.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Time when the event is called relatively from the beginning of the experience in milliseconds.
        /// </summary>
        public float Time { get; set; }
        /// <summary>
        /// Time when the event is called relatively to the main event of the subBloc in milliseconds.
        /// </summary>
        public float RelativeTime { get; set; }
    }
}