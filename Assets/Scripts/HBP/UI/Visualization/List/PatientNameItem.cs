using UnityEngine;

namespace HBP.UI.Visualization
{
    public class PatientNameItem : Tools.Unity.Lists.Item<Data.Patient>
    {
        protected override void SetObject(Data.Patient objectToSet)
        {
            GetComponentInChildren<UnityEngine.UI.Text>().text = objectToSet.Name;
        }
    }
}

