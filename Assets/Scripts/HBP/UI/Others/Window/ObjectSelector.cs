using Tools.Unity.Lists;

namespace HBP.UI
{
    public class ObjectSelector<T> : SavableWindow
    {
        #region Properties
        protected SelectableList<T> m_List;
        public T[] Objects
        {
            get
            {
                return m_List.Objects;
            }
            set
            {
                m_List.Objects = value;
            }
        }
        public T[] ObjectsSelected
        {
            get
            {
                return m_List.ObjectsSelected;
            }
            set
            {
                m_List.ObjectsSelected = value;
            }
        }
        public bool MultiSelection
        {
            get
            {
                return m_List.MultiSelection;
            }
            set
            {
                m_List.MultiSelection = value;
            }
        }
        public bool OpenModifierWhenSave { get; set; }

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_List.Interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_List.Initialize();
            m_List.OnSelectionChanged.AddListener(UpdateButtonState);
            UpdateButtonState();

            base.Initialize();
        }
        void UpdateButtonState()
        {
            m_SaveButton.interactable = Interactable && ObjectsSelected.Length > 0;
        }
        #endregion
    }
}