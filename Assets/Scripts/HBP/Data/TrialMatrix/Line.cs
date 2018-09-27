using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;

namespace HBP.Data.TrialMatrix
{
    public class Line
    {
        #region Attributs
        public Localizer.Bloc Bloc { get; set; }
        string m_Site;
        public float[] NormalizedValues { get; set; }
        #endregion

        #region Constructor
        public Line(Localizer.Bloc bloc, string site)
        {
            Bloc = bloc;
            m_Site = site;
            //NormalizedValues = bloc.ValuesBySite[site];
        }
        #endregion

        #region Public Methods
        public static IEnumerable<Line> MakeLines(Experience.Protocol.Bloc bloc, IEnumerable<Localizer.Bloc> blocs, Module3D.Site site)
        {
            return SortLines(bloc, (from b in blocs select new Line(b, site.Information.FullCorrectedID)));
        }
        public void UpdateValues()
        {
            //NormalizedValues = Bloc.NormalizedValuesBySite[m_Site];
        }
        #endregion

        #region Private Methods
        static IOrderedEnumerable<Line> SortLines(Experience.Protocol.Bloc bloc, IEnumerable<Line> lines)
        {
            // TODO
            //string l_sort = bloc.Sort;
            //string[] l_sortCommands = l_sort.SplitInParts(2).ToArray();
            IOrderedEnumerable<Line> l_linesSorted = lines.OrderBy(x => 1);
            //for (int i = 0; i < l_sortCommands.Length; i++)
            //{
            //    string l_sortCommand = l_sortCommands[i];
            //    if (l_sortCommand.Length == 2)
            //    {
            //        int p = int.Parse(l_sortCommand[1].ToString());

            //        if (l_sortCommand[0] == 'C')
            //        {
            //            if (p == 0)
            //            {
            //                // TODO.
            //                l_linesSorted = l_linesSorted.ThenBy(t => bloc.MainEvent.Codes[0]);
            //            }
            //            else
            //            {
            //                if ((p - 1) < bloc.SecondaryEvents.Count)
            //                {
            //                    // TODO.
            //                    l_linesSorted = l_linesSorted.ThenBy(t => bloc.SecondaryEvents[p-1].Codes[0]);
            //                }
            //            }
            //        }
            //        else if (l_sortCommand[0] == 'L')
            //        {
            //            if (p == 0)
            //            {
            //                l_linesSorted = l_linesSorted.ThenBy(t => t.Bloc.PositionByEvent[bloc.MainEvent]);
            //            }
            //            else
            //            {
            //                if ((p - 1) < bloc.SecondaryEvents.Count)
            //                {
            //                    l_linesSorted = l_linesSorted.ThenBy(t => t.Bloc.PositionByEvent[bloc.SecondaryEvents[p - 1]]);
            //                }
            //            }
            //        }
            //    }
            //}
            return l_linesSorted;
        }
        #endregion
    }
}