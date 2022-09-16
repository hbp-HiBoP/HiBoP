using System.Linq;
using HBP.Core.Enums;
using HBP.Core.DLL;

namespace HBP.Core.Data
{
    /// <summary>
    /// A Structure containing all the statistic data about subTrial in a specific channel.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>InformationsByEvent</b></term>
    /// <description>Informations by event.</description>
    /// </item>
    /// <item>
    /// <term><b>Unit</b></term>
    /// <description>Unit of data contained in this channel.</description>
    /// </item>
    /// <item>
    /// <term><b>Values</b></term>
    /// <description>Values for this sub-trial contained in this channel.</description>
    /// </item>
    /// <item>
    /// <term><b>Found</b></term>
    /// <description>True if the sub-trial is found, False otherwise.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public struct ChannelSubTrialStat
    {
        #region Properties
        /// <summary>
        /// Statistical Values for this sub-trial contained in this channel.
        /// </summary>
        public float[] Values { get; set; }
        /// <summary>
        /// Standard error of the mean for this sub-trial contained in this channel.
        /// </summary>
        public float[] SEM { get; set; }
        public int TotalNumberOfSubTrials { get; set; }
        public int NumberOfFoundSubTrials { get; set; }
        #endregion

        #region Constructors
        public ChannelSubTrialStat(float[] values, float[] sem) : this()
        {
            Values = values;
            SEM = sem;
        }
        public ChannelSubTrialStat(ChannelSubTrial[] subTrials,bool[] isValid, AveragingType averaging)
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
                    case AveragingType.Mean:
                        for (int i = 0; i < numberOfSamples; i++)
                        {
                            Values[i] = valuesMatrix[i].Mean();
                            SEM[i] = valuesMatrix[i].SEM();
                        }
                        break;
                    case AveragingType.Median:
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

        #region Public Methods
        public void Clear()
        {
            Values = new float[0];
            SEM = new float[0];
            TotalNumberOfSubTrials = 0;
            NumberOfFoundSubTrials = 0;
        }
        #endregion
    }
}
