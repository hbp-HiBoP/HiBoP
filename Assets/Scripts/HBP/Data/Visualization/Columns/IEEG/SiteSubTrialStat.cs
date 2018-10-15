using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public struct SiteSubTrialStat
    {
        #region Properties
        public float[] Values { get; set; }
        public float[] SEM { get; set; }
        public int TotalNumberOfSubTrials { get; set; }
        public int NumberOfFoundSubTrials { get; set; }
        #endregion

        #region Constructors
        public SiteSubTrialStat(float[] values, float[] sem) : this()
        {
            Values = values;
            SEM = sem;
        }
        public SiteSubTrialStat(SiteSubTrial[] subTrials,bool[] isValid, Enums.AveragingType averaging)
        {
            if (subTrials.Length > 0)
            {
                TotalNumberOfSubTrials = subTrials.Length;            
                NumberOfFoundSubTrials = subTrials.Count(s => s.Found);

                int numberOfValidTrial = isValid.Count((v) => v);
                int numberOfSamples = subTrials.FirstOrDefault((s) => s.Found).Values.Length;

                Values = new float[numberOfSamples];
                SEM = new float[numberOfSamples];
                float[][] valuesMatrix = new float[numberOfSamples][];
                for (int i = 0; i < numberOfSamples; i++)
                {
                    valuesMatrix[i] = new float[numberOfValidTrial];
                    for (int t = 0, p=0; t < TotalNumberOfSubTrials; t++)
                    {
                        if(isValid[t])
                        {
                            valuesMatrix[i][p] = subTrials[t].Values[i];
                            p++;
                        }
                    }
                }
                switch (averaging)
                {
                    case Enums.AveragingType.Mean:
                        for (int i = 0; i < numberOfSamples; i++)
                        {
                            Values[i] = valuesMatrix[i].Mean();
                            SEM[i] = valuesMatrix[i].SEM();
                        }
                        break;
                    case Enums.AveragingType.Median:
                        for (int i = 0; i < numberOfSamples; i++)
                        {
                            Values[i] = valuesMatrix[i].Median();
                            SEM[i] = valuesMatrix[i].SEM();
                        }
                        break;
                }
            }
            else
            {
                Values = new float[0];
                SEM = new float[0];
                NumberOfFoundSubTrials = 0;
                TotalNumberOfSubTrials = 0;
            }
        }
        #endregion
    }
}
