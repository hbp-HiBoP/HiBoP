using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public abstract class ObjectSelector<T> : DialogWindow
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

        public enum SelectionType { Single, Multi}
        [SerializeField] SelectionType m_Selection;
        public SelectionType Selection
        {
            get
            {
                return m_Selection;
            }
            set
            {
                m_Selection = value;
                switch (value)
                {
                    case SelectionType.Single:
                        List.ItemSelection = SelectableList<T>.SelectionType.SingleItem;
                        break;
                    case SelectionType.Multi:
                        List.ItemSelection = SelectableList<T>.SelectionType.MultipleItems;
                        break;
                    default:
                        break;
                }
            }
        }
        public bool OpenModifiers { get; set; }

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
            List.OnSelect.AddListener((obj) => UpdateButtonState());
            List.OnDeselect.AddListener((obj) => UpdateButtonState());
            UpdateButtonState();

            base.Initialize();
        }
        void UpdateButtonState()
        {
            m_OKButton.interactable = Interactable && ObjectsSelected.Length > 0;
        }
        #endregion
    }
}