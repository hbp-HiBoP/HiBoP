using HBP.Module3D;
using System;
using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    public class MEGvData : Data
    {
        #region Properties
        public MRI FMRI { get; private set; }
        #endregion

        #region Constructors
        public MEGvData(MEGvDataInfo dataInfo)
        {
            if (dataInfo.DataContainer is Container.Nifti niftiDataContainer)
            {
                FMRI = new MRI(dataInfo.Name, niftiDataContainer.File);
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
        }
        #endregion

        #region Public Methods
        public override void Clear()
        {
            FMRI = null;
        }
        #endregion
    }
}