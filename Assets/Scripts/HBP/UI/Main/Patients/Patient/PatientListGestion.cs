using HBP.UI.Tools.Lists;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Data;
using System.Linq;

namespace HBP.UI.Main
{
    public class PatientListGestion : ListGestion<Patient>
    {
        #region Properties

        [SerializeField] PatientList m_List;
        public override ActionableList<Patient> List => m_List;

        [SerializeField] PatientCreator m_ObjectCreator;
        public override ObjectCreator<Patient> ObjectCreator => m_ObjectCreator;

        #endregion

        #region Public Methods

        /// <summary>
        /// Callback executed when a ObjectModifier is modified.
        /// </summary>
        /// <param name="obj">Object modified</param>
        protected override void OnSaveModifier(Patient obj)
        {
            if (List.Objects.Any(c => c.Name == obj.Name && !c.Equals(obj)))
            {
                int count = 1;
                string name = string.Format("{0}({1})", obj.Name, count);
                while (List.Objects.Any(c => c.Name == name))
                {
                    count++;
                    name = string.Format("{0}({1})", obj.Name, count);
                }

                obj.Name = name;
            }

            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
        }

        /// <summary>
        /// Callback executed when a object is created.
        /// </summary>
        /// <param name="obj">Object created</param>
        protected override void OnObjectCreated(Patient obj)
        {
            if (List.Objects.Contains(obj))
            {
                List.UpdateObject(obj);
            }
            else
            {
                if (List.Objects.Any(c => c.Name == obj.Name && !c.Equals(obj)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", obj.Name, count);
                    while (List.Objects.Any(c => c.Name == name))
                    {
                        count++;
                        name = string.Format("{0}({1})", obj.Name, count);
                    }

                    obj.Name = name;
                }

                List.Add(obj);
            }

            HasBeenModified = true;
        }

        #endregion
    }
}
