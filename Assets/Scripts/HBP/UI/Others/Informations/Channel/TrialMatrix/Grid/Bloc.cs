using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using data = HBP.Data.TrialMatrix.Grid;

namespace HBP.UI.TrialMatrix.Grid
{
    public class Bloc : MonoBehaviour
    {
        #region Properties
        data.Bloc m_Data;
        public data.Bloc Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                Set(value, Colors, Limits);
            }
        }

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
                    foreach (var channelBloc in ChannelBlocs)
                    {
                        foreach (var subBloc in channelBloc.SubBlocs)
                        {
                            subBloc.Limits = value;
                        }
                    }
                }
            }
        }

        Color[] m_Colors;
        public Color[] Colors
        {
            get
            {
                return m_Colors;
            }
            set
            {
                m_Colors = value;
                foreach (var channelBloc in ChannelBlocs)
                {
                    channelBloc.Colors = value;
                }
            }
        }

        public bool IsHovered
        {
            get
            {
                return ChannelBlocs.Any(channelBloc => channelBloc.IsHovered);
            }
        }
        public UnityEvent OnChangeIsHovered;

        public ChannelBloc ChannelBlocHovered
        {
            get
            {
                return ChannelBlocs.FirstOrDefault(c => c.IsHovered);
            }
        }
        List<ChannelBloc> m_ChannelBlocs = new List<ChannelBloc>();
        public ReadOnlyCollection<ChannelBloc> ChannelBlocs
        {
            get
            {
                return new ReadOnlyCollection<ChannelBloc>(m_ChannelBlocs);
            }
        }

        [SerializeField] GameObject m_ChannelBlocPrefab;
        [SerializeField] RectTransform m_ChannelBlocContainer;
        #endregion

        #region Public Methods
        public void Set(data.Bloc data, Color[] colors, Vector2 limits)
        {
            m_Data = data;
            Title = data.Title;
            Clear();
            foreach (var channelBloc in data.ChannelBlocs)
            {
                AddChannelBloc(channelBloc, colors, limits);
            }
        }
        #endregion

        #region Private Methods
        void AddChannelBloc(data.ChannelBloc data, Color[] colors, Vector2 limits)
        {
            GameObject gameObject = Instantiate(m_ChannelBlocPrefab, m_ChannelBlocContainer);
            ChannelBloc channelBloc = gameObject.GetComponent<ChannelBloc>();
            channelBloc.Set(data, colors, limits);
            channelBloc.OnChangeIsHovered.AddListener(() => OnChangeIsHovered.Invoke());
            m_ChannelBlocs.Add(channelBloc);
        }
        void Clear()
        {
            foreach (Transform child in m_ChannelBlocContainer)
            {
                Destroy(child.gameObject);
            }
            m_ChannelBlocs = new List<ChannelBloc>();
        }
        #endregion
    }
}

