using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using d = HBP.Data.TrialMatrix;
using System.Collections.ObjectModel;

namespace HBP.UI.TrialMatrix
{
    public class Bloc : MonoBehaviour
    {
        #region Properties
        public GameObject SubBlocPrefab;
        public RectTransform SubBlocsRectTransform;
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

        List<SubBloc> blocs = new List<SubBloc>();
        public ReadOnlyCollection<SubBloc> SubBlocs { get { return new ReadOnlyCollection<SubBloc>(blocs); } }
        #endregion

        #region Public Methods
        public void Set(d.Bloc blocs,Texture2D colorMap,Vector2 limits)
        {
            //Title = blocs[0].ProtocolBloc.Name;
            //foreach (d.Bloc bloc in blocs)
            //{
            //    AddBloc(bloc, colorMap,limits);
            //}

            //if (blocs.Length < max)
            //{
            //    int blocEmptyToAdd = max - blocs.Length;
            //    for (int i = 0; i < blocEmptyToAdd; i++)
            //    {
            //        AddEmpty();
            //    }
            //}
        }
        public void SelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            foreach(SubBloc l_bloc in blocs)
            {
                if(l_bloc.Data.ProtocolBloc == bloc)
                {
                    l_bloc.SelectLines(lines,additive);
                }
            }
        }
        #endregion

        #region Private Methods        
        void AddSubBloc(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            SubBloc instantiateBloc = (Instantiate(SubBlocPrefab) as GameObject).GetComponent<SubBloc>();
            instantiateBloc.Set(bloc, colorMap, limits);
            instantiateBloc.transform.SetParent(SubBlocsRectTransform);
            blocs.Add(instantiateBloc);
        }
        void AddEmpty()
        {
            GameObject emptyBloc = new GameObject("Empty bloc", new System.Type[] { typeof(Image) });
            emptyBloc.transform.SetParent(SubBlocsRectTransform);
            emptyBloc.GetComponent<Image>().color = Color.black;
        }
        #endregion
    }
}
