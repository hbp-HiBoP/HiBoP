using UnityEngine.UI;

namespace HBP.UI.Lists
{
    /// <summary>
    /// Component to display a string in a list.
    /// </summary>
    public class LabelItem : Item<string>
    {
        #region Properties
        /// <summary>
        /// UI Text to display the string.
        /// </summary>
        public Text Text;
        /// <summary>
        /// String to display.
        /// </summary>
        public override string Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                Text.text = value;
            }
        }
        #endregion
    }
}