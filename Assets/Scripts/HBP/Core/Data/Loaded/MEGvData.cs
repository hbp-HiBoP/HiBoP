using System;

namespace HBP.Core.Data
{
    public class MEGvData : Data
    {
        #region Properties
        public MRI FMRI { get; private set; } = new MRI();
        public MRI Mask { get; set; } = new MRI();
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

            if (!string.IsNullOrEmpty(dataInfo.MaskDataContainer.File))
            {
                Mask = new MRI(dataInfo.Name + "_mask", dataInfo.MaskDataContainer.File);
            }
        }
        #endregion

        #region Public Methods
        public override void Clear()
        {
            FMRI = null;
            Mask = null;
        }
        #endregion
    }
}