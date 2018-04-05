using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using d = HBP.Data.TrialMatrix;
using UnityEngine.Profiling;
using Tools.Unity;

namespace HBP.UI.TrialMatrix
{
    public class Line : MonoBehaviour
    {
        #region Properties
        public GameObject BlocPrefab;
        public RectTransform BlocsRectTransform;
        string m_title;
        public string Title
        {
            get { return m_title; }
            set
            {
                m_title = value;
                OnChangeTitle.Invoke(value);
            }
        }
        public StringEvent OnChangeTitle;

        List<Bloc> blocs = new List<Bloc>();
        public Bloc[] Blocs { get { return blocs.ToArray(); } }
        #endregion

        #region Public Methods
        public void Set(d.Bloc[] blocs,int max,Texture2D colorMap,Vector2 limits)
        {
            Title = blocs[0].ProtocolBloc.Name;
            foreach (d.Bloc bloc in blocs)
            {
                AddBloc(bloc, colorMap,limits);
            }

            if (blocs.Length < max)
            {
                int blocEmptyToAdd = max - blocs.Length;
                for (int i = 0; i < blocEmptyToAdd; i++)
                {
                    AddEmpty();
                }
            }
        }
        public void SelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            foreach(Bloc l_bloc in blocs)
            {
                if(l_bloc.Data.ProtocolBloc == bloc)
                {
                    l_bloc.SelectLines(lines,additive);
                }
            }
        }
        #endregion

        #region Private Methods        
        void AddBloc(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            Bloc instantiateBloc = (Instantiate(BlocPrefab) as GameObject).GetComponent<Bloc>();
            instantiateBloc.Set(bloc, colorMap, limits);
            instantiateBloc.transform.SetParent(BlocsRectTransform);
            blocs.Add(instantiateBloc);
        }
        void AddEmpty()
        {
            GameObject emptyBloc = new GameObject("Empty bloc", new System.Type[] { typeof(Image) });
            emptyBloc.transform.SetParent(BlocsRectTransform);
            emptyBloc.GetComponent<Image>().color = Color.black;
        }
        #endregion
    }
}
