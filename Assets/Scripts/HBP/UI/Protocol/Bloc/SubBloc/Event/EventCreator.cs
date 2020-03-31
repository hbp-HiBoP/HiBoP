using p = HBP.Data.Experience.Protocol;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to create Events.
    /// </summary>
    public class EventCreator : ObjectCreator<p.Event>
    {
        #region Properties
        [SerializeField] Data.Enums.MainSecondaryEnum m_Type;
        /// <summary>
        /// Default event type created.
        /// </summary>
        public Data.Enums.MainSecondaryEnum Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                m_Type = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Create event from scratch with the default type.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new p.Event(Type));
        }
        #endregion
    }
}