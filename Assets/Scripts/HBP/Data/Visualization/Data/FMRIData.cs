using HBP.Data.Experience.Dataset;
using HBP.Module3D;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class FMRIData
    {
        #region Properties
        public List<Module3D.FMRI> FMRIs { get; set; } = new List<Module3D.FMRI>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<FMRIDataInfo> columnData)
        {
            foreach (FMRIDataInfo dataInfo in columnData)
            {
                Experience.Dataset.FMRIData data = DataManager.GetData(dataInfo) as Experience.Dataset.FMRIData;
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

