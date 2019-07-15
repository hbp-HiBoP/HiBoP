using System.Collections;
using System.Collections.Generic;
using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ColumnInformation : ColumnOverlayElement
    {
        #region Properties
        [SerializeField] private Text m_Protocol;
        [SerializeField] private Text m_Bloc;
        [SerializeField] private Text m_Dataset;
        [SerializeField] private Text m_Data;
        #endregion

        #region Public Methods
        public override void Setup(Base3DScene scene, Column3D column, Column3DUI columnUI)
        {
            base.Setup(scene, column, columnUI);

            if (column is Column3DIEEG columnIEEG)
            {
                m_Protocol.text = columnIEEG.ColumnIEEGData.Dataset.Protocol.Name;
                m_Bloc.text = columnIEEG.ColumnIEEGData.Bloc.Name;
                m_Dataset.text = columnIEEG.ColumnIEEGData.Dataset.Name;
                m_Data.text = columnIEEG.ColumnIEEGData.DataName;
                IsActive = true;
            }
            else if (column is Column3DCCEP columnCCEP)
            {
                m_Protocol.text = columnCCEP.ColumnCCEPData.Dataset.Protocol.Name;
                m_Bloc.text = columnCCEP.ColumnCCEPData.Bloc.Name;
                m_Dataset.text = columnCCEP.ColumnCCEPData.Dataset.Name;
                m_Data.text = columnCCEP.ColumnCCEPData.DataName;
                IsActive = true;
            }
            else
            {
                IsActive = false;
            }
        }
        #endregion
    }
}