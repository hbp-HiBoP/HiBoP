using UnityEngine;
using HBP.Display.Informations;
using HBP.UI.Tools.Graphs;

namespace HBP.UI.Module3D.Informations
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
                if (column.Data is IEEGData || column.Data is CCEPData)
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
                    Core.Tools.TimeWindow window = megData.Window;
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