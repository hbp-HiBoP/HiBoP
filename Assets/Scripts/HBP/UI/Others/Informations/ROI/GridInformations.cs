using System.Collections.Generic;
using UnityEngine;
using data = HBP.Data.Informations;
using System.Linq;
using Tools.Unity.Graph;
using HBP.Data.Experience.Protocol;
using System;
using HBP.Data.Experience.Dataset;
using HBP.Data.Informations;
using HBP.Data;
using Tools.CSharp;

namespace HBP.UI.Informations
{
    public class GridInformations : MonoBehaviour
    {
        #region Properties
        [SerializeField] ChannelStruct[] m_Channels;
        [SerializeField] Column[] m_Columns;
        [SerializeField] GraphsGrid m_Grid;
        #endregion

        #region Public Methods
        public void Display(ChannelStruct[] channels, Column[] columns)
        {
            m_Channels = channels;
            m_Columns = columns;

            if (isActiveAndEnabled)
            {
                m_Grid.Display(channels, columns);
            }
        }
        #endregion
    }
}