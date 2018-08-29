using System.Collections.Generic;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class ProtocolListGestion : ListGestion<Data.Experience.Protocol.Protocol>
    {
        #region Properties
        [SerializeField]new ProtocolList List;
        public override List<Data.Experience.Protocol.Protocol> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(ProtocolList.Sorting.Descending);
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