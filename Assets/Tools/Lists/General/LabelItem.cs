namespace Tools.Unity.Lists
{
    public class LabelItem : Item<string>
    {
        #region Properties
        public override string Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                GetComponent<UnityEngine.UI.Text>().text = value;
            }
        }
        #endregion
    }
}


