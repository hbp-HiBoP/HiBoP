using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HBP.Core.Data
{
    public class StaticData : Data
    {
        #region Properties
        public string[] Labels { get; set; }
        public Dictionary<string, float[]> ValuesByChannel { get; set; }
        #endregion

        #region Constructors
        public StaticData() : this(new string[0], new Dictionary<string, float[]>())
        {
        }
        public StaticData(string[] labels, Dictionary<string, float[]> valuesByChannel)
        {
            Labels = labels;
            ValuesByChannel = valuesByChannel;
        }
        public StaticData(StaticDataInfo dataInfo) : this()
        {
            if (dataInfo.DataContainer is Container.CSV csvDataContainer)
            {
                Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                if (new FileInfo(csvDataContainer.SavedFile).Exists)
                {
                    using (StreamReader sr = new StreamReader(csvDataContainer.SavedFile))
                    {
                        string line = sr.ReadLine();
                        Labels = csvParser.Split(line).Skip(1).ToArray();
                        while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                        {
                            string[] splits = csvParser.Split(line);
                            ValuesByChannel.Add(splits[0], splits.Skip(1).Select(s => NumberExtension.ParseFloat(s)).ToArray());
                        }
                    }
                }
                else
                {
                    throw new Exception("CSV file does not exist");
                }
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
        }
        #endregion

        #region Public Methods
        public override void Clear()
        {
            ValuesByChannel.Clear();
        }
        #endregion
    }
}