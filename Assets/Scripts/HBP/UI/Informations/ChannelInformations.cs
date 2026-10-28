using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using data = HBP.Data.Informations;

namespace HBP.UI.Informations
{
    public class ChannelInformations : MonoBehaviour
    {
        #region Properties

        [SerializeField] data.ChannelStruct[] m_Channels;
        [SerializeField] data.Column[] m_Columns;

        [SerializeField] GraphZone m_GraphZone;
        [SerializeField] TrialMatrixZone m_TrialMatrixZone;

        #endregion

        private InformationPresentation presentation;
        private long renderedRevision = -1;
        private long renderedSamples = -1;

        internal void Bind(InformationPresentation value)
        {
            if (ReferenceEquals(presentation, value)) return;
            presentation = value;
            renderedRevision = renderedSamples = -1;
            m_GraphZone.BindPresentation(value);
            m_TrialMatrixZone.Presentation = value;
        }

        internal void DisplayPresentation()
        {
            if (presentation == null || !isActiveAndEnabled || renderedRevision == presentation.ContentRevision) return;
            renderedRevision = presentation.ContentRevision;
            m_Channels = presentation.Channels;
            m_Columns = presentation.Columns;
            if (renderedSamples != presentation.SamplesRevision)
            {
                renderedSamples = presentation.SamplesRevision;
                m_TrialMatrixZone.Display(presentation.Matrices);
            }

            m_GraphZone.Display(m_Channels, m_Columns);
        }

        private void LateUpdate() => DisplayPresentation();

        #region Public Methods

        public void SetMaxNumberOfTrialMatrixColumn(int max)
        {
            m_GraphZone.CreateGraphPool(max);
        }

        public void Display(data.ChannelStruct[] channels, data.Column[] columns)
        {
            m_Channels = channels;
            m_Columns = columns;

            if (isActiveAndEnabled)
            {
                m_TrialMatrixZone.Display(channels, columns.Select(column => column.Data).ToArray());
                m_GraphZone.Display(channels, m_Columns);
            }
        }

        public void DisplayTrialMatrices(data.ChannelStruct[] channels, data.Column[] columns)
        {
            m_Channels = channels;
            m_Columns = columns;

            if (isActiveAndEnabled)
            {
                m_TrialMatrixZone.Display(channels, columns.Select(column => column.Data).ToArray());
            }
        }

        public void DisplayGraphs(data.ChannelStruct[] channels, data.Column[] columns)
        {
            m_Channels = channels;
            m_Columns = columns;

            if (isActiveAndEnabled)
            {
                m_GraphZone.Display(channels, columns);
            }
        }

        public void Refresh()
        {
            Display(m_Channels, m_Columns);
        }

        public void UpdateTimeVisibility(bool visible)
        {
            m_GraphZone.UpdateTimeVisibility(visible);
        }

        public void UpdateTime(data.Column column, Core.Data.SubBloc subBloc, float currentTime)
        {
            m_GraphZone.UpdateTime(column, subBloc, currentTime);
        }

        public void SetLocalizersCurves(Dictionary<data.ChannelStruct, List<LocalizerCurveData>> curves)
        {
            m_GraphZone.SetLocalizersCurves(curves);
        }

        #endregion

        #region Private Methods

        void OnEnable()
        {
            if (presentation != null)
            {
                renderedRevision = renderedSamples = -1;
                DisplayPresentation();
                return;
            }

            if (m_Channels != null && m_Columns != null)
            {
                m_TrialMatrixZone.Display(m_Channels, m_Columns.Select(column => column.Data).ToArray());
                m_GraphZone.Display(m_Channels, m_Columns);
            }
        }

        #endregion
    }
}
