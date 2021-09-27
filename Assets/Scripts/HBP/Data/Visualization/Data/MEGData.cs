using HBP.Data.Experience.Dataset;
using HBP.Module3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class MEGData
    {
        #region Properties
        public List<Tuple<Module3D.FMRI, Patient>> FMRIs { get; set; } = new List<Tuple<Module3D.FMRI, Patient>>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<MEGDataInfo> columnData)
        {
            foreach (MEGDataInfo dataInfo in columnData)
            {
                Experience.Dataset.MEGData data = DataManager.GetData(dataInfo) as Experience.Dataset.MEGData;
                FMRIs.Add(new Tuple<Module3D.FMRI, Patient>(new Module3D.FMRI(data.FMRI), dataInfo.Patient));
            }
        }
        public void Unload()
        {
            foreach (var fmri in FMRIs)
            {
                fmri.Item1.Clean();
            }
            FMRIs.Clear();
        }
        #endregion
    }
}

