using HBP.UI.Informations.TrialMatrix;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    [RequireComponent(typeof(ChannelBloc)), RequireComponent(typeof(RectTransform)), RequireComponent(typeof(LayoutElement))]
    public class ChannelBlocSizer : MonoBehaviour
    {
        #region Properties
        private ChannelBloc m_ChannelBloc;
        private LayoutElement m_LayoutElement;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ChannelBloc = GetComponent<ChannelBloc>();
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
                var length = m_ChannelBloc.Data.SubBlocs.First(s => s.SubBlocProtocol == m_ChannelBloc.Data.Bloc.MainSubBloc).SubTrials.Length;
                m_LayoutElement.preferredHeight = length * 5;
                m_LayoutElement.flexibleHeight = length;
            }
            else
            {
                m_LayoutElement.flexibleHeight = 1;
            }
        }
        #endregion
    }
}