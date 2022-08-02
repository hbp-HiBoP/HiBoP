using UnityEngine;
using HBP.Core.Data.Enums;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to create Events.
    /// </summary>
    public class EventCreator : ObjectCreator<Core.Data.Event>
    {
        #region Properties
        [SerializeField] MainSecondaryEnum m_Type;
        /// <summary>
        /// Default event type created.
        /// </summary>
        public MainSecondaryEnum Type
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
            OpenModifier(new Core.Data.Event(Type));
        }
        #endregion
    }
}