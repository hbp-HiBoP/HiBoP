using UnityEngine;
using UnityEngine.UI;

namespace HBP.Theme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Selectable")]
    public class Selectable : Settings
    {
        #region Properties
        public UnityEngine.UI.Selectable.Transition Transition;
        public ColorBlock Colors;
        public SpriteState SpriteState;
        public AnimationTriggers AnimationTriggers;
        #endregion

        #region Public Methods
        public override void Set(GameObject gameObject)
        {
            UnityEngine.UI.Selectable selectable = gameObject.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable)
            {
                selectable.colors = Colors.Colors;
                selectable.spriteState = SpriteState;
                selectable.animationTriggers = AnimationTriggers;
            }
        }
        #endregion

    }
}