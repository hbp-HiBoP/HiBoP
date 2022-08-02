using UnityEngine;
using data = HBP.Data.Informations;
using Tools.Unity.Graph;
using HBP.Data.Informations;

namespace HBP.UI.Informations
{
    public class GridInformations : MonoBehaviour
    {
        #region Properties
        [SerializeField] ChannelStruct[] m_Channels;
        [SerializeField] Column[] m_Columns;
        [SerializeField] GraphsGrid m_Grid;
        public GraphsGrid Grid { get { return m_Grid; } }
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
                if (column.Data is data.IEEGData || column.Data is data.CCEPData)
                {
                    Core.Data.SubBloc mainSubBloc = column.Data.Bloc.MainSubBloc;
                    if (mainSubBloc.Window.Start < abscissaDisplayRange.x)
                    {
                        abscissaDisplayRange = new Vector2(mainSubBloc.Window.Start, abscissaDisplayRange.y);
                    }
                    if (mainSubBloc.Window.End > abscissaDisplayRange.y)
                    {
                        abscissaDisplayRange = new Vector2(abscissaDisplayRange.x, mainSubBloc.Window.End);
                    }
                }
                else if (column.Data is MEGData megData)
                {
                    Tools.CSharp.Window window = megData.Window;
                    if (window.Start < abscissaDisplayRange.x)
                    {
                        abscissaDisplayRange = new Vector2(window.Start, abscissaDisplayRange.y);
                    }
                    if (window.End > abscissaDisplayRange.y)
                    {
                        abscissaDisplayRange = new Vector2(abscissaDisplayRange.x, window.End);
                    }
                }
            }
            m_Grid.AbscissaDisplayRange = abscissaDisplayRange;
        }
        #endregion
    }
}