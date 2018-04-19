using UnityEngine;

namespace NewTheme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Horizontal or Vertical Layout Group")]
    public class HorizontalOrVerticalLayoutGroup : Settings
    {
        #region Properties
        public RectOffset Padding;
        public float Spacing;
        public TextAnchor ChildAlignment;
        public bool ChildControlWidth;
        public bool ChildControlHeight;
        public bool ChildForceExpandWidth;
        public bool ChildForceExpandHeight;
        #endregion

        #region Public Methods
        public override void Set(GameObject gameObject)
        {
            Debug.Log(gameObject.name);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup horizontalOrVerticalLayoutGroup = gameObject.GetComponent<UnityEngine.UI.HorizontalOrVerticalLayoutGroup>();
            horizontalOrVerticalLayoutGroup.padding = Padding;
            horizontalOrVerticalLayoutGroup.spacing = Spacing;
            horizontalOrVerticalLayoutGroup.childAlignment = ChildAlignment;
            horizontalOrVerticalLayoutGroup.childControlWidth = ChildControlWidth;
            horizontalOrVerticalLayoutGroup.childControlHeight = ChildControlHeight;
            horizontalOrVerticalLayoutGroup.childForceExpandWidth = ChildForceExpandWidth;
            horizontalOrVerticalLayoutGroup.childForceExpandHeight = ChildForceExpandHeight;
        }
        #endregion
    }
}