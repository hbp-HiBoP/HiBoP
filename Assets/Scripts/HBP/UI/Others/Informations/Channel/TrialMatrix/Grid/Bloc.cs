using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using data = HBP.Data.TrialMatrix.Grid;

namespace HBP.UI.TrialMatrix.Grid
{
    public class Bloc : MonoBehaviour
    {
        #region Properties
        string m_Title;
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (value != null && value != m_Title)
                {
                    m_Title = value;
                    OnChangeTitle.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeTitle;

        Vector2 m_Limits;
        public Vector2 Limits
        {
            get
            {
                return m_Limits;
            }
            set
            {
                if (value != null && value != m_Limits)
                {
                    m_Limits = value;
                    foreach (var bloc in BlocByChannel.Values)
                    {
                        foreach (var subBloc in bloc.SubBlocs)
                        {
                            subBloc.Limits = value;
                        }
                    }
                }
            }
        }

        public Dictionary<data.ChannelBloc, ChannelBloc> BlocByChannel { get; set; }

        [SerializeField] GameObject m_ChannelBlocPrefab;
        [SerializeField] GameObject m_FillerPrefab;
        [SerializeField] RectTransform m_ChannelBlocContainer;
        #endregion

        #region Public Methods
        public void Set(data.Bloc data, Texture2D colormap, Vector2 limits, IEnumerable<Tuple<HBP.Data.Experience.Protocol.SubBloc[], Tools.CSharp.Window>> timeLimitsByColumn)
        {
            Title = data.Title;
            Clear();
            foreach (var channelBloc in data.ChannelBlocs)
            {
                if(channelBloc.Found)
                {
                    AddChannelBloc(channelBloc, colormap, limits, timeLimitsByColumn);
                }
                else
                {
                    AddFiller();
                }
            }
        }
        #endregion

        #region Private Methods
        void AddChannelBloc(data.ChannelBloc data, Texture2D colormap, Vector2 limits, IEnumerable<Tuple<HBP.Data.Experience.Protocol.SubBloc[], Tools.CSharp.Window>> timeLimitsByColumn)
        {
            GameObject gameObject = Instantiate(m_ChannelBlocPrefab, m_ChannelBlocContainer);
            ChannelBloc channelBloc = gameObject.GetComponent<ChannelBloc>();
            channelBloc.Set(data, colormap, limits, timeLimitsByColumn);
            BlocByChannel.Add(data, channelBloc);
        }
        void AddFiller()
        {
            GameObject gameObject = Instantiate(m_FillerPrefab, m_ChannelBlocContainer);
        }

        void Clear()
        {
            foreach (Transform child in m_ChannelBlocContainer)
            {
                Destroy(child.gameObject);
            }
            BlocByChannel = new Dictionary<data.ChannelBloc, ChannelBloc>();
        }
        #endregion
    }
}

