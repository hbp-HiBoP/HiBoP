using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace HBP.Core.Object3D
{
    public class DiFuMoInformation
    {
        #region Structs
        public struct Labels
        {
            #region Propreties
            public int Component { get; private set; }
            public string Name { get; private set; }
            public string YeoNetworks7 { get; private set; }
            public string YeoNetworks17 { get; private set; }
            public float GM { get; private set; }
            public float WM { get; private set; }
            public float CSF { get; private set; }
            #endregion

            #region Constructors
            public Labels(int component, string name, string yeoNetworks7, string yeoNetworks17, float gm, float wm, float csf)
            {
                Component = component;
                Name = name;
                YeoNetworks7 = yeoNetworks7;
                YeoNetworks17 = yeoNetworks17;
                GM = gm;
                WM = wm;
                CSF = csf;
            }
            #endregion
        }
        #endregion

        #region Properties
        public List<Labels> AllLabels { get; } = new List<Labels>();
        #endregion

        #region Constructors
        public DiFuMoInformation(string csvFile)
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
                        int component = splits.Length > 0 ? int.Parse(splits[0]) - 1 : 0;
                        string name = splits.Length > 1 ? splits[1].TrimStart(' ', '"').TrimEnd('"') : "";
                        string yeoNetworks7 = splits.Length > 2 ? splits[2].TrimStart(' ', '"').TrimEnd('"') : "";
                        string yeoNetworks17 = splits.Length > 3 ? splits[3].TrimStart(' ', '"').TrimEnd('"') : "";
                        float gm = splits.Length > 4 && global::Tools.CSharp.NumberExtension.TryParseFloat(splits[4], out float gmValue) ? gmValue : 0;
                        float wm = splits.Length > 5 && global::Tools.CSharp.NumberExtension.TryParseFloat(splits[5], out float wmValue) ? wmValue : 0;
                        float csf = splits.Length > 6 && global::Tools.CSharp.NumberExtension.TryParseFloat(splits[6], out float csfValue) ? csfValue : 0;
                        AllLabels.Add(new Labels(component, name, yeoNetworks7, yeoNetworks17, gm, wm, csf));
                    }
                }
                AllLabels.Sort(delegate (Labels a, Labels b) { return a.Component.CompareTo(b.Component); });
            }
        }
        #endregion

        #region Public Methods
        public Labels GetLabels(int component)
        {
            return AllLabels[component];
        }
        #endregion
    }
}