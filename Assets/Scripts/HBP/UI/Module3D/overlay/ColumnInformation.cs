using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Overlay element to display some information about the column
    /// </summary>
    public class ColumnInformation : ColumnOverlayElement
    {
        #region Properties
        /// <summary>
        /// Displays the protocol used in this column
        /// </summary>
        [SerializeField] private Text m_Protocol;
        /// <summary>
        /// Displays the bloc used in this column
        /// </summary>
        [SerializeField] private Text m_Bloc;
        /// <summary>
        /// Displays the dataset used in this column
        /// </summary>
        [SerializeField] private Text m_Dataset;
        /// <summary>
        /// Displays the data used in this column
        /// </summary>
        [SerializeField] private Text m_Data;
        #endregion

        #region Public Methods
        /// <summary>
        /// Setup the overlay element
        /// </summary>
        /// <param name="scene">Associated 3D scene</param>
        /// <param name="column">Associated 3D column</param>
        /// <param name="columnUI">Parent UI column</param>
        public override void Setup(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Setup(scene, column, columnUI);

            IsActive = true;
            if (column is Column3DIEEG columnIEEG)
            {
                m_Protocol.text = columnIEEG.ColumnIEEGData.Dataset.Protocol.Name;
                m_Bloc.text = columnIEEG.ColumnIEEGData.Bloc.Name;
                m_Dataset.text = columnIEEG.ColumnIEEGData.Dataset.Name;
                m_Data.text = columnIEEG.ColumnIEEGData.DataName;
            }
            else if (column is Column3DCCEP columnCCEP)
            {
                m_Protocol.text = columnCCEP.ColumnCCEPData.Dataset.Protocol.Name;
                m_Bloc.text = columnCCEP.ColumnCCEPData.Bloc.Name;
                m_Dataset.text = columnCCEP.ColumnCCEPData.Dataset.Name;
                m_Data.text = columnCCEP.ColumnCCEPData.DataName;
            }
            else if (column is Column3DFMRI fmriColumn)
            {
                m_Protocol.text = fmriColumn.ColumnFMRIData.Dataset.Protocol.Name;
                m_Bloc.text = "FMRI";
                m_Dataset.text = fmriColumn.ColumnFMRIData.Dataset.Name;
                m_Data.text = fmriColumn.SelectedFMRI.Name;
                fmriColumn.OnChangeSelectedFMRI.AddListener(() =>
                {
                    m_Data.text = fmriColumn.SelectedFMRI.Name;
                });
            }
            else
            {
                IsActive = false;
            }
        }
        #endregion
    }
}