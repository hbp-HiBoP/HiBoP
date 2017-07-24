using HBP.Data;

namespace HBP.UI.Visualization
{
    public class PatientNameItem : Tools.Unity.Lists.Item<Data.Patient>
    {
        #region Properties
        public override Patient Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                GetComponentInChildren<UnityEngine.UI.Text>().text = value.Name;

            }
        }
        #endregion
    }
}

