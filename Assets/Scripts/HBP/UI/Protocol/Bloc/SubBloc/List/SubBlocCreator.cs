using HBP.Data.Experience.Protocol;
using Tools.Unity.Components;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to create subBlocs.
    /// </summary>
    public class SubBlocCreator : ObjectCreator<SubBloc>
    {
        #region Properties
        [SerializeField] Data.Enums.MainSecondaryEnum m_Type;
        /// <summary>
        /// Default subBloc type created.
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
        /// Create subBlocs from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new SubBloc(m_Type));
        }
        #endregion
    }
}