using UnityEngine;
using HBP.Core.Enums;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to create subBlocs.
    /// </summary>
    public class SubBlocCreator : ObjectCreator<Core.Data.SubBloc>
    {
        #region Properties
        [SerializeField] MainSecondaryEnum m_Type;
        /// <summary>
        /// Default subBloc type created.
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
        /// Create subBlocs from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Core.Data.SubBloc(m_Type));
        }
        #endregion
    }
}