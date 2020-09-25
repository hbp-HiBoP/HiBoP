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
using HBP.Module3D;

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
        public void Display(ChannelStruct[] channels)
        {
            m_Channels = channels;

            if (isActiveAndEnabled)
            {
                m_Grid.Display(m_Channels, m_Columns);
            }
        }
        public void SetColumns(Column[] columns)
        {
            m_Columns = columns;
            Vector2 abscissaDisplayRange = new Vector2(float.MaxValue, float.MinValue);
            foreach (var column in m_Columns)
            {
                SubBloc mainSubBloc = column.Data.Bloc.MainSubBloc;
                if (mainSubBloc.Window.Start < abscissaDisplayRange.x)
                {
                    abscissaDisplayRange = new Vector2(mainSubBloc.Window.Start, abscissaDisplayRange.y);
                }
                if (mainSubBloc.Window.End > abscissaDisplayRange.y)
                {
                    abscissaDisplayRange = new Vector2(abscissaDisplayRange.x, mainSubBloc.Window.End);
                }
            }
            m_Grid.AbscissaDisplayRange = abscissaDisplayRange;
        }
        #endregion
    }
}