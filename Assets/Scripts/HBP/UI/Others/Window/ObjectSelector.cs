using HBP.UI.Lists;
using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Base abstract window to select one or multiple objects in a list of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObjectSelector<T> : DialogWindow
    {
        #region Properties
        /// <summary>
        /// List in the UI.
        /// </summary>
        protected abstract SelectableList<T> List { get; }
        /// <summary>
        /// Possible objects.
        /// </summary>
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
        /// <summary>
        /// Objects selected.
        /// </summary>
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
        /// <summary>
        /// Selection type.
        /// </summary>
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
        /// <summary>
        /// True if open window modifier return the objects selected.
        /// </summary>
        public bool OpenModifiers { get; set; }

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            List.OnSelect.AddListener((obj) => UpdateButtonState());
            List.OnDeselect.AddListener((obj) => UpdateButtonState());
            UpdateButtonState();

            base.Initialize();
        }
        /// <summary>
        /// Update button state.
        /// </summary>
        void UpdateButtonState()
        {
            m_OKButton.interactable = Interactable && ObjectsSelected.Length > 0;
        }
        #endregion
    }
}