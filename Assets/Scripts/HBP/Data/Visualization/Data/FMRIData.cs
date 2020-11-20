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
        public List<MRI3D> FMRIs { get; set; } = new List<MRI3D>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<FMRIDataInfo> columnData)
        {
            foreach (FMRIDataInfo dataInfo in columnData)
            {
                Experience.Dataset.FMRIData data = DataManager.GetData(dataInfo) as Experience.Dataset.FMRIData;
                FMRIs.Add(new MRI3D(data.FMRI));
            }
        }
        public void Unload()
        {
            FMRIs.Clear();
        }
        #endregion
    }
}

