using System.Collections.Generic;
using System.Linq;

namespace Tools.Unity.Graph
{
    public class CurveGroupData
    {
        #region Properties
        public string Name { get; set; }
        public List<CurveData> Curves { get; set; }
        #endregion

        #region Constructors
        public CurveGroupData(string name)
        {
            Name = name;
            Curves = new List<CurveData>();
        }
        #endregion

        #region Public Methods
        public CurveData GetCurveByName(string name)
        {
            return Curves.FirstOrDefault(c => c.name == name);
        }
        #endregion
    }
}