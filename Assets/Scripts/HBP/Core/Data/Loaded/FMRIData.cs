using System;

namespace HBP.Core.Data
{
    public class FMRIData : Data
    {
        #region Properties
        public MRI FMRI { get; private set; }
        #endregion

        #region Constructors
        public FMRIData(FMRIDataInfo dataInfo)
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
        public FMRIData(SharedFMRIDataInfo dataInfo)
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