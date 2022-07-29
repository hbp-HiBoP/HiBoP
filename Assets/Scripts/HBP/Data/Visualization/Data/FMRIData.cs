using HBP.Data.Experience.Dataset;
using HBP.Module3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class FMRIData
    {
        #region Properties
        public List<Tuple<Core.Object3D.FMRI, Patient>> FMRIs { get; set; } = new List<Tuple<Core.Object3D.FMRI, Patient>>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<FMRIDataInfo> columnData)
        {
            foreach (FMRIDataInfo dataInfo in columnData)
            {
                Experience.Dataset.FMRIData data = DataManager.GetData(dataInfo) as Experience.Dataset.FMRIData;
                FMRIs.Add(new Tuple<Core.Object3D.FMRI, Patient>(new Core.Object3D.FMRI(data.FMRI), dataInfo.Patient));
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

