using HBP.UI.Tools.Lists;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Data;
using HBP.Core.Interfaces;
using System.Linq;

namespace HBP.UI.Main
{
    public class DatasetListGestion : ListGestion<Core.Data.Dataset>
    {
        #region Properties
        [SerializeField] protected DatasetList m_List;
        public override ActionableList<Core.Data.Dataset> List => m_List;

        [SerializeField] protected DatasetCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Dataset> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Public Methods
        protected override void OnObjectCreated(Dataset obj)
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
                    while (List.Objects.OfType<INameable>().Any(c => c.Name == name))
                    {
                        count++;
                        name = string.Format("{0}({1})", obj.Name, count);
                    }
                    obj.Name = name;
                }
                List.Add(obj);
            }
        }
        #endregion
    }
}
