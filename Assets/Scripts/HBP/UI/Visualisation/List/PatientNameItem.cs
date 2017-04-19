using UnityEngine;

namespace HBP.UI.Visualisation
{
    public class PatientNameItem : Tools.Unity.Lists.ListItem<Data.Patient>
    {
        protected override void SetObject(Data.Patient objectToSet)
        {
            GetComponentInChildren<UnityEngine.UI.Text>().text = objectToSet.Name;
        }

        public override void Set(Data.Patient objectToSet, Rect rect)
        {
            Object = objectToSet;
        }
    }
}

