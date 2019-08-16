using System.Collections.ObjectModel;
using System.Collections.Generic;
using Tools.Unity.Components;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class ProtocolListGestion : ListGestion<d.Protocol>
    {
        #region Properties
        List<d.Protocol> m_ModifiedProtocols = new List<d.Protocol>();
        public ReadOnlyCollection<d.Protocol> ModifiedProtocols
        {
            get
            {
                return new ReadOnlyCollection<d.Protocol>(m_ModifiedProtocols);
            }
        }
        [SerializeField]new ProtocolList List;
        public override List<d.Protocol> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
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
        protected override void OnSaveModifier(ItemModifier<Data.Experience.Protocol.Protocol> modifier)
        {
            base.OnSaveModifier(modifier);
            m_ModifiedProtocols.Add(modifier.Item);
        }
        #endregion
    }
}