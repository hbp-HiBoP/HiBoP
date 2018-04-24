using Microsoft.VisualStudio.TestTools.UnitTesting;
using HBP.Data.Localizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBP.Data.Localizer.Tests
{
    [TestClass()]
    public class BlocTests
    {
        [TestMethod()]
        public void AverageTest()
        {
            // Set blocs
            Bloc bloc1 = new Bloc();
            Bloc bloc2 = new Bloc();
            Bloc bloc3 = new Bloc();
            Bloc meanBloc = new Bloc();
            Bloc medianBloc = new Bloc();
            Experience.Protocol.Event mainEvent = new Experience.Protocol.Event();
            string[] sites = new string[] { "A", "B", "C" };
            for (int s = 0; s < sites.Length; s++)
            {
                List<float> valuesBloc1 = new List<float>();
                List<float> valuesBloc2 = new List<float>();
                List<float> valuesBloc3 = new List<float>();
                List<float> valuesMeanBloc = new List<float>();
                List<float> valuesMedianBloc = new List<float>();

                List<float> baselineValuesBloc1 = new List<float>();
                List<float> baselineValuesBloc2 = new List<float>();
                List<float> baselineValuesBloc3 = new List<float>();
                List<float> baselineValuesMeanBloc = new List<float>();
                List<float> baselineValuesMedianBloc = new List<float>();

                List<float> normalizedValuesBloc1 = new List<float>();
                List<float> normalizedValuesBloc2 = new List<float>();
                List<float> normalizedValuesBloc3 = new List<float>();
                List<float> normalizedValuesMeanBloc = new List<float>();
                List<float> normalizedValuesMedianBloc = new List<float>();

                for (int v = 0; v < 100; v++)
                {
                    valuesBloc1.Add(-v + s);
                    baselineValuesBloc1.Add(-v + 100 + s);
                    normalizedValuesBloc1.Add(-v + 200 + s);

                    valuesBloc2.Add(2 * v + s);
                    baselineValuesBloc2.Add(2 * v + 100 + s);
                    normalizedValuesBloc2.Add(2 * v + 200 + s);

                    valuesBloc3.Add(8 * v + s);
                    baselineValuesBloc3.Add(8 * v + 100 + s);
                    normalizedValuesBloc3.Add(8 * v + 200 + s);

                    valuesMeanBloc.Add(3 * v + s);
                    baselineValuesMeanBloc.Add(3 * v + 100 + s);
                    normalizedValuesMeanBloc.Add(3 * v + 200 + s);

                    valuesMedianBloc.Add(2 * v + s);
                    baselineValuesMedianBloc.Add(2 * v + 100 + s);
                    normalizedValuesMedianBloc.Add(2 * v + 200 + s);
                }

                bloc1.ValuesBySite.Add(sites[s], valuesBloc1.ToArray());
                bloc1.BaseLineValuesBySite.Add(sites[s], baselineValuesBloc1.ToArray());
                bloc1.NormalizedValuesBySite.Add(sites[s], normalizedValuesBloc1.ToArray());

                bloc2.ValuesBySite.Add(sites[s], valuesBloc2.ToArray());
                bloc2.BaseLineValuesBySite.Add(sites[s], baselineValuesBloc2.ToArray());
                bloc2.NormalizedValuesBySite.Add(sites[s], normalizedValuesBloc2.ToArray());

                bloc3.ValuesBySite.Add(sites[s], valuesBloc3.ToArray());
                bloc3.BaseLineValuesBySite.Add(sites[s], baselineValuesBloc3.ToArray());
                bloc3.NormalizedValuesBySite.Add(sites[s], normalizedValuesBloc3.ToArray());

                meanBloc.ValuesBySite.Add(sites[s], valuesMeanBloc.ToArray());
                meanBloc.BaseLineValuesBySite.Add(sites[s], baselineValuesMeanBloc.ToArray());
                meanBloc.NormalizedValuesBySite.Add(sites[s], normalizedValuesMeanBloc.ToArray());

                medianBloc.ValuesBySite.Add(sites[s], valuesMedianBloc.ToArray());
                medianBloc.BaseLineValuesBySite.Add(sites[s], baselineValuesMedianBloc.ToArray());
                medianBloc.NormalizedValuesBySite.Add(sites[s], normalizedValuesMedianBloc.ToArray());
            }

            bloc1.PositionByEvent.Add(mainEvent, 2);
            bloc2.PositionByEvent.Add(mainEvent, 8);
            bloc3.PositionByEvent.Add(mainEvent, 80);
            meanBloc.PositionByEvent.Add(mainEvent, 30);
            medianBloc.PositionByEvent.Add(mainEvent, 8);


            Bloc[] blocs = new Bloc[] { bloc1, bloc2, bloc3 };
            // Result => Value : Mean || Event : Mean.
            Bloc meanResult = Bloc.Average(blocs, Preferences.UserPreferences.AveragingMode.Mean, Preferences.UserPreferences.AveragingMode.Mean);
            // Result => Value : Median || Event : Median.
            Bloc medianResult = Bloc.Average(blocs, Preferences.UserPreferences.AveragingMode.Median, Preferences.UserPreferences.AveragingMode.Median);

            // Compare
            foreach (var site in sites)
            {
                for (int v = 0; v < meanResult.ValuesBySite[site].Length; v++)
                {
                    Assert.AreEqual(meanBloc.ValuesBySite[site][v], meanResult.ValuesBySite[site][v]);
                    Assert.AreEqual(meanBloc.BaseLineValuesBySite[site][v], meanResult.BaseLineValuesBySite[site][v]);
                    Assert.AreEqual(meanBloc.NormalizedValuesBySite[site][v], meanResult.NormalizedValuesBySite[site][v]);
                    Assert.AreEqual(meanBloc.PositionByEvent[mainEvent], meanResult.PositionByEvent[mainEvent]);

                    Assert.AreEqual(medianBloc.ValuesBySite[site][v], medianResult.ValuesBySite[site][v]);
                    Assert.AreEqual(medianBloc.BaseLineValuesBySite[site][v], medianResult.BaseLineValuesBySite[site][v]);
                    Assert.AreEqual(medianBloc.NormalizedValuesBySite[site][v], medianResult.NormalizedValuesBySite[site][v]);
                    Assert.AreEqual(medianBloc.PositionByEvent[mainEvent], medianResult.PositionByEvent[mainEvent]);
                }
            }
        }
    }
}