using Cysharp.Threading.Tasks;
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
        public async UniTask LoadAsync(IEnumerable<FMRIDataInfo> columnData, IEnumerable<SharedFMRIDataInfo> sharedFMRIs)
        {
            foreach (FMRIDataInfo dataInfo in columnData)
            {
                Core.Data.FMRIData data = DataManager.GetData(dataInfo) as Core.Data.FMRIData;
                var fmri = new Object3D.FMRI(data.FMRI, data.Mask, false);
                await fmri.LoadAsync();
                FMRIs.Add(new Tuple<Object3D.FMRI, Patient>(fmri, dataInfo.Patient));
            }
            foreach (SharedFMRIDataInfo dataInfo in sharedFMRIs)
            {
                Core.Data.FMRIData data = DataManager.GetData(dataInfo) as Core.Data.FMRIData;
                var fmri = new Object3D.FMRI(data.FMRI, data.Mask, false);
                await fmri.LoadAsync();
                FMRIs.Add(new Tuple<Object3D.FMRI, Patient>(fmri, null));
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

