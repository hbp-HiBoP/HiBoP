using HBP.Core.Data;
using HBP.Core.Interfaces;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Main
{
    public class BasicBlocListManager : SubModifier<Protocol>, ISelectionCountable
    {
        #region Properties
        [SerializeField] private Transform m_BlocsContainer;
        [SerializeField] private GameObject m_BlocItemPrefab;

        private List<BasicBlocItem> m_BlocItems = new List<BasicBlocItem>();

        public override bool Interactable { get => base.Interactable; set => base.Interactable = value; }

        public int NumberOfSelectedObjects => m_BlocItems.Count(b => b.Selected);
        public int NumberOfObjects => m_BlocItems.Count;
        public int NumberOfFilteredObjects => m_BlocItems.Count;
        public bool CanSelectMultipleObjects => true;
        #endregion

        #region Events
        public GenericEvent<Bloc> OnAddBloc = new GenericEvent<Bloc>();
        UnityEvent ISelectionCountable.OnSelectionChanged { get; } = new UnityEvent();
        #endregion

        #region Private Methods
        protected override void SetFields(Protocol objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            foreach (var blocItem in m_BlocItems)
            {
                Destroy(blocItem.gameObject);
            }
            m_BlocItems.Clear();
            foreach (var bloc in objectToDisplay.OrderedBlocs)
            {
                GameObject newBlocItemObject = Instantiate(m_BlocItemPrefab, m_BlocsContainer);
                BasicBlocItem newBlocItem = newBlocItemObject.GetComponent<BasicBlocItem>();
                newBlocItem.Bloc = bloc;
                newBlocItem.OnValueChanged.AddListener(value => OnSelectionUpdate());
                m_BlocItems.Add(newBlocItem);
            }
            UpdateBlocOrders();
            OnSelectionUpdate();
        }
        private void UpdateBlocOrders()
        {
            for (int i = 0; i < m_BlocItems.Count; i++)
            {
                m_BlocItems[i].Bloc.Order = i;
            }
        }
        private void OnSelectionUpdate()
        {
            (this as ISelectionCountable).OnSelectionChanged.Invoke();
        }
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
            newBlocItem.OnValueChanged.AddListener(value => OnSelectionUpdate());
            m_BlocItems.Add(newBlocItem);
            Object.Blocs.Add(newBlocItem.Bloc);
            OnAddBloc.Invoke(newBlocItem.Bloc);
            OnSelectionUpdate();
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
            UpdateBlocOrders();
            OnSelectionUpdate();
        }
        #endregion
    }
}