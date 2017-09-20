using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Toggle))]
    public abstract class SelectableItem<T> : Item<T>
    {
        #region Properties
        public virtual Toggle.ToggleEvent OnChangeSelected { get { return GetComponent<Toggle>().onValueChanged; } }
        public virtual bool selected
        {
            get { return GetComponent<Toggle>().isOn; }
            set { GetComponent<Toggle>().isOn = value; }
        }
        public override bool interactable
        {
            get { return GetComponent<Toggle>().interactable; }
            set { GetComponent<Toggle>().interactable = value; }
        }
        #endregion

        #region Public Methods
        public void ChangeSelectionState()
        {
            selected = !selected;
        }
        #endregion
    }
}