using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Main
{
    public class BasicBlocListManager : SubModifier<Protocol>
    {
        #region Properties
        [SerializeField] private Transform m_BlocsContainer;
        [SerializeField] private GameObject m_BlocItemPrefab;

        private List<BasicBlocItem> m_BlocItems = new List<BasicBlocItem>();

        public override bool Interactable { get => base.Interactable; set => base.Interactable = value; }
        #endregion

        #region Events
        public GenericEvent<Bloc> OnAddBloc = new GenericEvent<Bloc>();
        #endregion

        #region Public Methods
        public void AddBloc()
        {
            GameObject newBlocItemObject = Instantiate(m_BlocItemPrefab, m_BlocsContainer);
            BasicBlocItem newBlocItem = newBlocItemObject.GetComponent<BasicBlocItem>();
            newBlocItem.Bloc = new Bloc()
            {
                Name = "",
                Order = m_BlocItems.Count,
                SubBlocs = new List<SubBloc>()
                {
                    new SubBloc()
                    {
                        Name = "Main",
                        Order = 0,
                        Type = Core.Enums.MainSecondaryEnum.Main,
                        Events = new List<Core.Data.Event>()
                        {
                            new Core.Data.Event(Core.Enums.MainSecondaryEnum.Main)
                            {
                                Name = "",
                                CodesString = ""
                            }
                        }
                    }
                }
            };
            m_BlocItems.Add(newBlocItem);
            Object.Blocs.Add(newBlocItem.Bloc);
            OnAddBloc.Invoke(newBlocItem.Bloc);
        }
        public void RemoveSelectedBlocs()
        {
            for (int i = m_BlocItems.Count - 1; i >= 0; i--)
            {
                if (m_BlocItems[i].Selected)
                {
                    Object.Blocs.Remove(m_BlocItems[i].Bloc);
                    Destroy(m_BlocItems[i].gameObject);
                    m_BlocItems.RemoveAt(i);
                }
            }
            for (int i = 0; i < m_BlocItems.Count; i++)
            {
                m_BlocItems[i].Bloc.Order = i;
            }
        }
        #endregion
    }
}