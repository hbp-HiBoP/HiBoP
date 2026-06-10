using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations.TrialMatrix
{
    [RequireComponent(typeof(ChannelBloc)), RequireComponent(typeof(RectTransform)), RequireComponent(typeof(LayoutElement))]
    public class ChannelBlocSizer : MonoBehaviour
    {
        #region Properties
        private ChannelBloc m_ChannelBloc;
        private RectTransform m_RectTransform;
        private LayoutElement m_LayoutElement;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ChannelBloc = GetComponent<ChannelBloc>();
            m_RectTransform = GetComponent<RectTransform>();
            m_LayoutElement = GetComponent<LayoutElement>();

            m_ChannelBloc.OnSet.AddListener(SetSize);
        }
        void OnRectTransformDimensionsChange()
        {
            SetSize();
        }
        #endregion

        #region Public Methods
        public void SetSize()
        {
            if (m_ChannelBloc.Data.IsFound)
            {
                CanvasScalerHandler canvasScalerHandler = GetComponentInParent<CanvasScalerHandler>();
                float scale = canvasScalerHandler ? canvasScalerHandler.Scale : 1;
                switch (PersistentDataManager.UserPreferences.Visualization.TrialMatrix.SubBlocFormat)
                {
                    case BlocFormatType.TrialHeight:
                        m_LayoutElement.preferredHeight = PersistentDataManager.UserPreferences.Visualization.TrialMatrix.TrialHeight * m_ChannelBloc.Data.SubBlocs.First(s => s.SubBlocProtocol == m_ChannelBloc.Data.Bloc.MainSubBloc).SubTrials.Length / scale;
                        m_LayoutElement.flexibleHeight = -1;
                        break;
                    case BlocFormatType.TrialRatio:
                        m_LayoutElement.preferredHeight = PersistentDataManager.UserPreferences.Visualization.TrialMatrix.TrialRatio * m_RectTransform.rect.width * m_ChannelBloc.Data.SubBlocs.First(s => s.SubBlocProtocol == m_ChannelBloc.Data.Bloc.MainSubBloc).SubTrials.Length / scale;
                        m_LayoutElement.flexibleHeight = -1;
                        break;
                    case BlocFormatType.BlocRatio:
                        m_LayoutElement.preferredHeight = PersistentDataManager.UserPreferences.Visualization.TrialMatrix.BlocRatio * m_RectTransform.rect.width / scale;
                        m_LayoutElement.flexibleHeight = -1;
                        break;
                    case BlocFormatType.ProtocolRatio:
                        var length = m_ChannelBloc.Data.SubBlocs.First(s => s.SubBlocProtocol == m_ChannelBloc.Data.Bloc.MainSubBloc).SubTrials.Length;
                        m_LayoutElement.preferredHeight = length * 5;
                        m_LayoutElement.flexibleHeight = length;
                        break;
                }
            }
            else
            {
                m_LayoutElement.flexibleHeight = 1;
            }
        }
        #endregion
    }
}