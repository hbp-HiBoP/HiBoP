using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Interfaces;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class BasicBlocListManager : SubModifier<Protocol>, ISelectionCountable
    {
        #region Properties
        [SerializeField] private Transform m_BlocsContainer;
        [SerializeField] private GameObject m_BlocItemPrefab;
        [SerializeField] private GameObject m_DropIndicatorPrefab;

        private List<BasicBlocItem> m_BlocItems = new List<BasicBlocItem>();
        private GameObject m_CurrentDropIndicator;
        private int m_CurrentDropIndex = -1;
        private DraggableItem m_CurrentDraggedItem;

        private float m_ItemHeight = 32;

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
        public override void Initialize()
        {
            base.Initialize();

            m_ItemHeight = m_BlocItemPrefab.GetComponent<LayoutElement>().preferredHeight;
        }
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
                
                DraggableItem draggableItem = newBlocItem.GetComponent<DraggableItem>();
                if (draggableItem != null)
                {
                    draggableItem.OnDragStart.AddListener(OnItemDragStart);
                    draggableItem.OnDragMove.AddListener(OnItemDragMove);
                    draggableItem.OnDragEnd.AddListener(OnItemDragEnd);
                }
                
                m_BlocItems.Add(newBlocItem);
            }
            UpdateBlocOrders();
            OnSelectionUpdate();
        }
        private void AddBloc(Bloc bloc)
        {
            GameObject newBlocItemObject = Instantiate(m_BlocItemPrefab, m_BlocsContainer);
            BasicBlocItem newBlocItem = newBlocItemObject.GetComponent<BasicBlocItem>();
            newBlocItem.Bloc = bloc;
            newBlocItem.OnValueChanged.AddListener(value => OnSelectionUpdate());

            DraggableItem draggableItem = newBlocItem.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                draggableItem.OnDragStart.AddListener(OnItemDragStart);
                draggableItem.OnDragMove.AddListener(OnItemDragMove);
                draggableItem.OnDragEnd.AddListener(OnItemDragEnd);
            }

            m_BlocItems.Add(newBlocItem);
            Object.Blocs.Add(newBlocItem.Bloc);
            OnAddBloc.Invoke(newBlocItem.Bloc);
            OnSelectionUpdate();
        }
        private void AddBlocsFromExampleFile(IEnumerable<Bloc> blocs)
        {
            foreach (var bloc in blocs)
            {
                AddBloc(bloc);
            }
        }
        private void UpdateBlocOrders()
        {
            for (int i = 0; i < m_BlocItems.Count; i++)
            {
                m_BlocItems[i].Bloc.Order = i;
                m_BlocItems[i].Refresh();
            }
        }
        private void OnSelectionUpdate()
        {
            (this as ISelectionCountable).OnSelectionChanged.Invoke();
        }
        private int GetDropIndexFromPosition(Vector2 screenPosition)
        {
            RectTransform containerRect = m_BlocsContainer as RectTransform;
            if (containerRect == null) return -1;

            Vector2 localPosition;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                containerRect, screenPosition, null, out localPosition))
                return -1;
            
            float distanceFromTop = -localPosition.y;
            
            if (distanceFromTop < 0)
                return 0;
            
            int index = Mathf.FloorToInt(distanceFromTop / m_ItemHeight);
            
            float remainder = distanceFromTop % m_ItemHeight;
            if (remainder > m_ItemHeight / 2)
                index++;
            
            return Mathf.Clamp(index, 0, m_BlocItems.Count);
        }
        private void ShowDropIndicator(int index)
        {
            if (m_DropIndicatorPrefab == null) return;

            HideDropIndicator();

            m_CurrentDropIndicator = Instantiate(m_DropIndicatorPrefab, m_BlocsContainer);
            m_CurrentDropIndex = index;

            m_CurrentDropIndicator.transform.SetSiblingIndex(index);
        }
        private void HideDropIndicator()
        {
            if (m_CurrentDropIndicator != null)
            {
                Destroy(m_CurrentDropIndicator);
                m_CurrentDropIndicator = null;
                m_CurrentDropIndex = -1;
            }
        }
        private void OnItemDragStart(DraggableItem item)
        {
            m_CurrentDraggedItem = item;
        }
        private void OnItemDragMove(DraggableItem item, Vector2 screenPosition)
        {
            if (item != m_CurrentDraggedItem) return;
            
            int dropIndex = GetDropIndexFromPosition(screenPosition);
            
            if (dropIndex != m_CurrentDropIndex)
            {
                ShowDropIndicator(dropIndex);
            }
        }
        private void OnItemDragEnd(DraggableItem draggedItem, Vector2 screenPosition)
        {
            if (draggedItem != m_CurrentDraggedItem) return;
            
            int dropIndex = GetDropIndexFromPosition(screenPosition);
            
            HideDropIndicator();
            m_CurrentDraggedItem = null;

            BasicBlocItem draggedBlocItem = draggedItem.GetComponent<BasicBlocItem>();
            if (draggedBlocItem == null) return;

            int currentIndex = m_BlocItems.IndexOf(draggedBlocItem);
            if (currentIndex == -1) return;

            if (dropIndex > currentIndex)
                dropIndex--;

            if (dropIndex == currentIndex) return;

            m_BlocItems.RemoveAt(currentIndex);
            m_BlocItems.Insert(dropIndex, draggedBlocItem);

            Object.Blocs.Clear();
            Object.Blocs.AddRange(m_BlocItems.Select(item => item.Bloc));

            draggedItem.transform.SetSiblingIndex(dropIndex);

            UpdateBlocOrders();
        }
        #endregion

        #region Public Methods
        public void AddNewBloc()
        {
            AddBloc(new Bloc()
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
            });
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
        public async void OpenBlocsImporterWindow()
        {
            var filePath = await FileBrowser.GetExistingFileNameAsync(new string[] { Elan.POS_EXTENSION[1..], Micromed.MICROMED_EXTENSION[1..], BrainVision.HEADER_EXTENSION[1..], FIF.FIF_EXTENSION[1..], EDF.EDF_EXTENSION[1..] }, "Select a data file containing events");
            if (string.IsNullOrEmpty(filePath)) return;

            var window = WindowsManager.Open("Basic bloc importer window", GetComponentInParent<Window>()).GetComponent<BasicBlocImporterWindow>();
            window.FilePath = filePath;
            window.OnBlocsImported.AddListener(AddBlocsFromExampleFile);
            WindowsReferencer.Add(window);
        }
        #endregion
    }
}