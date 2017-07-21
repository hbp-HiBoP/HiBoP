using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Toggle))]
    public abstract class SelectableItem<T> : Item<T>
    {
        #region Properties
        public GenericEvent<T, bool> onSelectionChanged { get; set; }
        public virtual bool selected
        {
            get { return GetComponent<Toggle>().isOn; }
            set { GetComponent<Toggle>().isOn = value; }
        }
        public virtual bool interactable
        {
            get { return GetComponent<Toggle>().interactable; }
            set { GetComponent<Toggle>().interactable = value; }
        }
        public override T Object
        {
            get { return base.Object; }
            set
            {
                base.Object = value;
                Toggle toggle = GetComponent<Toggle>();
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((isOn) => onSelectionChanged.Invoke(value, isOn));
            }
        }
        #endregion
    }
}