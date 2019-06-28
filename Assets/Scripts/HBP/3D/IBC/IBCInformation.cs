using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HBP.Module3D.IBC
{
    public class IBCInformation
    {
        #region Structs
        public struct Labels
        {
            public string PrettyName { get; private set; }
            public string ControlCondition { get; private set; }
            public string TargetCondition { get; private set; }

            public Labels(string prettyName, string controlCondition, string targetCondition)
            {
                PrettyName = prettyName;
                ControlCondition = controlCondition;
                TargetCondition = targetCondition;
            }
        }
        #endregion

        #region Properties
        private Dictionary<string, Labels> m_LabelsByRawName = new Dictionary<string, Labels>();
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
                        string rawName = splits.Length > 1 ? splits[1].TrimStart(' ', '"').TrimEnd('"') : "";
                        string prettyName = splits.Length > 2 ? splits[2].TrimStart(' ', '"').TrimEnd('"') : "";
                        string controlCondition = splits.Length > 3 ? splits[3].TrimStart(' ', '"').TrimEnd('"') : "";
                        string targetCondition = splits.Length > 4 ? splits[4].TrimStart(' ', '"').TrimEnd('"') : "";
                        m_LabelsByRawName.Add(rawName, new Labels(prettyName, controlCondition, targetCondition));
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public Labels GetLabels(string rawName)
        {
            if (m_LabelsByRawName.TryGetValue(rawName, out Labels labels))
            {
                return labels;
            }
            return new Labels("Unknown", "Unknown Condition", "Unknown Target");
        }
        #endregion
    }
}