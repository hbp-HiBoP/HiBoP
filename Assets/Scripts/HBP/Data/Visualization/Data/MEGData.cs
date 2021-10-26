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
        public void Load(IEnumerable<PatientDataInfo> columnData)
        {
            foreach (PatientDataInfo dataInfo in columnData)
            {
                if (dataInfo is MEGvDataInfo vDataInfo)
                {
                    MEGvData data = DataManager.GetData(vDataInfo) as MEGvData;
                    FMRIs.Add(new Tuple<Module3D.FMRI, Patient>(new Module3D.FMRI(data.FMRI), vDataInfo.Patient));
                }
                else if (dataInfo is MEGcDataInfo cDataInfo)
                {

                }
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

