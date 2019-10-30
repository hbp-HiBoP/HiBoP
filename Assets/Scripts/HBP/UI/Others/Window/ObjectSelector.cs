using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public abstract class ObjectSelector<T> : SavableWindow
    {
        #region Properties
        protected abstract SelectableList<T> List { get; }
        public T[] Objects
        {
            get
            {
                return List.Objects.ToArray();
            }
            set
            {
                List.Set(value);
            }
        }
        public T[] ObjectsSelected
        {
            get
            {
                return List.ObjectsSelected;
            }
            set
            {
                List.ObjectsSelected = value;
            }
        }
        public bool MultiSelection
        {
            get
            {
                return List.MultiSelection;
            }
            set
            {
                List.MultiSelection = value;
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
                List.Interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            List.OnSelectionChanged.AddListener(UpdateButtonState);
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