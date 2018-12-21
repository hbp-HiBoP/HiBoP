using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    public class LabelItem : Item<string>
    {
        #region Properties
        public Text Text;
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