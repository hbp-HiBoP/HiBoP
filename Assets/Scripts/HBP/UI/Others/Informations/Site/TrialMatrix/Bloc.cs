using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using d = HBP.Data.TrialMatrix;
using System.Collections.ObjectModel;
using System.Linq;

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

        List<int> m_SelectedTrials = new List<int>();
        public int[] SelectedTrials
        {
            get
            {
                return m_SelectedTrials.ToArray();
            }
            set
            {
                m_SelectedTrials = value.ToList();
            }
        }

        List<SubBloc> m_SubBlocs = new List<SubBloc>();
        public ReadOnlyCollection<SubBloc> SubBlocs { get { return new ReadOnlyCollection<SubBloc>(m_SubBlocs); } }
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
        public void SelectLines(int[] trials,bool additive)
        {
            foreach(SubBloc subBloc in m_SubBlocs)
            {
                subBloc.SelectTrials(trials, additive);
            }
        }
        #endregion

        #region Private Methods        
        void AddSubBloc(d.Bloc bloc,Texture2D colorMap,Vector2 limits)
        {
            SubBloc instantiateBloc = (Instantiate(SubBlocPrefab) as GameObject).GetComponent<SubBloc>();
            instantiateBloc.Set(bloc, colorMap, limits);
            instantiateBloc.transform.SetParent(SubBlocsRectTransform);
            m_SubBlocs.Add(instantiateBloc);
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
