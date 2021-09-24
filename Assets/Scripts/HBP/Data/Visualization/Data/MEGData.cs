using HBP.Data.Experience.Dataset;
using HBP.Module3D;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class MEGData
    {
        #region Properties
        public List<Module3D.FMRI> FMRIs { get; set; } = new List<Module3D.FMRI>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<MEGDataInfo> columnData)
        {
            foreach (MEGDataInfo dataInfo in columnData)
            {
                Experience.Dataset.MEGData data = DataManager.GetData(dataInfo) as Experience.Dataset.MEGData;
                FMRIs.Add(new Module3D.FMRI(data.FMRI));
            }
        }
        public void Unload()
        {
            foreach (var fmri in FMRIs)
            {
                fmri.Clean();
            }
            FMRIs.Clear();
        }
        #endregion
    }
}

