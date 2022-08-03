using UnityEngine;

namespace HBP.Theme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Layout Element")]
    public class LayoutElement : Settings
    {
        #region Properties
        public bool IgnoreLayout = false;
        public float MinWidth = -1;
        public float MinHeight = -1;
        public float PreferredWidth = -1;
        public float PreferredHeight = -1;
        public float FlexibleWidth = -1;
        public float FlexibleHeight = -1;
        #endregion

        #region Public Methods
        public override void Set(GameObject gameObject)
        {
            UnityEngine.UI.LayoutElement layoutElement = gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layoutElement)
            {              
                layoutElement.ignoreLayout = IgnoreLayout;
                layoutElement.minWidth = MinWidth;
                layoutElement.minHeight = MinHeight;
                layoutElement.preferredWidth = PreferredWidth;
                layoutElement.preferredHeight = PreferredHeight;
                layoutElement.flexibleWidth = FlexibleWidth;
                layoutElement.flexibleHeight = FlexibleHeight;
            }
        }
        #endregion

    }
}