using UnityEngine;
using d = HBP.Data.Patient;

namespace HBP.UI
{
    public class PatientNameItem : Tools.Unity.Lists.ListItem<d.Patient>
    {
        protected override void SetObject(d.Patient objectToSet)
        {
            GetComponentInChildren<UnityEngine.UI.Text>().text = objectToSet.Name;
        }

        public override void Set(d.Patient objectToSet, Rect rect)
        {
            Object = objectToSet;
        }
    }
}

