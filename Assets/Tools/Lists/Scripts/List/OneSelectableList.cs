using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(ToggleGroup))]
    public class OneSelectableList<T> : SelectableList<T>
    {
        #region Properties
        ToggleGroup m_toggleGroup;
        #endregion
        protected override void Set(T objectToSet, ListItem<T> listItem )
        {
            base.Set(objectToSet, listItem);
            listItem.GetComponent<Toggle>().group = m_toggleGroup;
        }

        void Start()
        {
            m_toggleGroup = GetComponent<ToggleGroup>();
        }
    }
}