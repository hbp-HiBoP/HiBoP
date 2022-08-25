using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Global information for IBC functional atlas (correspondance table between files name and 
    /// </summary>
    public class IBCInformation
    {
        #region Structs
        /// <summary>
        /// Structure containing the labels for a contrast
        /// </summary>
        public struct Labels
        {
            #region Propreties
            public int Index { get; set; }
            public string Task { get; private set; }
            public string Contrast { get; set; }
            public string PrettyName { get; private set; }
            public string ControlCondition { get; private set; }
            public string TargetCondition { get; private set; }
            #endregion

            #region Constructors
            public Labels(int index, string task, string contrast, string prettyName, string controlCondition, string targetCondition)
            {
                Index = index;
                Task = task;
                Contrast = contrast;
                PrettyName = prettyName;
                ControlCondition = controlCondition;
                TargetCondition = targetCondition;
            }
            #endregion
        }
        #endregion

        #region Properties
        public List<Labels> AllLabels { get; } = new List<Labels>();
        #endregion

        #region Constructors
        public IBCInformation(string csvFile)
        {
            Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            if (new FileInfo(csvFile).Exists)
            {
                using (StreamReader sr = new StreamReader(csvFile))
                {
                    string line = sr.ReadLine();
                    while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                    {
                        string[] splits = csvParser.Split(line);
                        int index = splits.Length > 0 ? int.Parse(splits[0]) - 1 : 0;
                        string task = splits.Length > 1 ? splits[1].TrimStart(' ', '"').TrimEnd('"') : "";
                        string contrast = splits.Length > 2 ? splits[2].TrimStart(' ', '"').TrimEnd('"') : "";
                        string prettyName = splits.Length > 3 ? splits[3].TrimStart(' ', '"').TrimEnd('"') : "";
                        string controlCondition = splits.Length > 4 ? splits[4].TrimStart(' ', '"').TrimEnd('"') : "";
                        string targetCondition = splits.Length > 5 ? splits[5].TrimStart(' ', '"').TrimEnd('"') : "";
                        AllLabels.Add(new Labels(index, task, contrast, prettyName, controlCondition, targetCondition));
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the <see cref="Labels"/> object corresponding to the raw file name
        /// </summary>
        /// <param name="rawName">Raw name (from the file name)</param>
        /// <returns>Corresponding Labels object</returns>
        public Labels GetLabels(int index)
        {
            return AllLabels[index];
        }
        #endregion
    }
}