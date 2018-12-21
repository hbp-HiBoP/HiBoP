using System.Collections.Generic;
using Tools.Unity.Components;

namespace HBP.UI.Anatomy
{
    public class PatientListGestion : ListGestion<Data.Patient>
    {
        #region Properties
        public new PatientList List;
        public override List<Data.Patient> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByName(PatientList.Sorting.Descending);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
        #endregion
    }
}