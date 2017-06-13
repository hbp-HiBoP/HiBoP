using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;

namespace HBP.Data.Localizer
{
    public class Bloc
	{
        #region Properties
        public Dictionary<Experience.Protocol.Event, int> PositionByEvent;
        public Dictionary<Anatomy.Site, float[]> ValuesBySite;
        #endregion

        #region Constructor
        public Bloc(Dictionary<Experience.Protocol.Event,int> positionByEvent,Dictionary<Anatomy.Site,float[]> valuesBySite)
		{
            PositionByEvent = positionByEvent;
            ValuesBySite = valuesBySite;
		}
        public Bloc(): this (new Dictionary<Experience.Protocol.Event, int>(),new Dictionary<Anatomy.Site, float[]>())
        {
        }
        #endregion

        #region Public static Methods
        public static Bloc Average(Bloc[] blocs)
        {
            Bloc result = new Bloc();


            Dictionary<Experience.Protocol.Event, List<int>> positionsByEvent = new Dictionary<Experience.Protocol.Event, List<int>>();
            Dictionary<Anatomy.Site, List<float>[]> valuesBySite = new Dictionary<Anatomy.Site, List<float>[]>();
            foreach (Bloc bloc in blocs)
            {
                foreach (Experience.Protocol.Event evnt in bloc.PositionByEvent.Keys)
                {
                    if (!positionsByEvent.ContainsKey(evnt)) positionsByEvent.Add(evnt, new List<int>());
                    positionsByEvent[evnt].Add(bloc.PositionByEvent[evnt]);
                }
                foreach (Anatomy.Site site in bloc.ValuesBySite.Keys)
                {
                    if (!valuesBySite.ContainsKey(site)) valuesBySite.Add(site, new List<float>[bloc.ValuesBySite[site].Length]);
                    for (int v = 0; v < valuesBySite[site].Length; v++)
                    {
                        if (valuesBySite[site][v] == null) valuesBySite[site][v] = new List<float>();
                        valuesBySite[site][v].Add(bloc.ValuesBySite[site][v]);
                    }
                }
            }
            foreach (var item in positionsByEvent) result.PositionByEvent.Add(item.Key, item.Value.Median());
            foreach (var item in valuesBySite) result.ValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Median()).ToArray());
            return result;		
		}
		#endregion
	}
} 