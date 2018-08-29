using System.Collections.Generic;
using Tools.Unity.Components;

namespace HBP.UI.Anatomy
{
    public class PatientListGestion : ListGestion<Data.Patient>
    {
        public new PatientList List;
        public override List<Data.Patient> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(PatientList.Sorting.Descending);
            }
        }

        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
    }
}