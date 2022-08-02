using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class ProtocolListGestion : ListGestion<Core.Data.Protocol>
    {
        #region Properties
        [SerializeField] List<Core.Data.Protocol> m_ModifiedProtocols = new List<Core.Data.Protocol>();
        public ReadOnlyCollection<Core.Data.Protocol> ModifiedProtocols
        {
            get
            {
                return new ReadOnlyCollection<Core.Data.Protocol>(m_ModifiedProtocols);
            }
        }

        [SerializeField]protected ProtocolList m_List;
        public override Tools.Unity.Lists.ActionableList<Core.Data.Protocol> List => m_List;

        [SerializeField] protected ProtocolCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Protocol> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Public Methods
        protected override void OnSaveModifier(Core.Data.Protocol obj)
        {
            m_ModifiedProtocols.Add(obj);
            base.OnSaveModifier(obj);
        }
        protected override void OnObjectCreated(Core.Data.Protocol obj)
        {
            m_ModifiedProtocols.Add(obj);
            base.OnObjectCreated(obj);
        }
        #endregion
    }
}