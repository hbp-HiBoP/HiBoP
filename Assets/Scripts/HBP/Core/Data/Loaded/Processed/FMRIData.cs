using System;
using System.Collections.Generic;

namespace HBP.Core.Data.Processed
{
    public class FMRIData
    {
        #region Properties
        public List<Tuple<Object3D.FMRI, Patient>> FMRIs { get; set; } = new List<Tuple<Object3D.FMRI, Patient>>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<FMRIDataInfo> columnData)
        {
            foreach (FMRIDataInfo dataInfo in columnData)
            {
                Core.Data.FMRIData data = DataManager.GetData(dataInfo) as Core.Data.FMRIData;
                FMRIs.Add(new Tuple<Object3D.FMRI, Patient>(new Object3D.FMRI(data.FMRI), dataInfo.Patient));
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

