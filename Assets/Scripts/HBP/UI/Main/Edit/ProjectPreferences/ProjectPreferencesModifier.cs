using UnityEngine;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.Data.Database;
using System;
using Cysharp.Threading.Tasks;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify the project preferences.
    /// </summary>
    public class ProjectPreferencesModifier : ObjectModifier<ProjectPreferences>
    {
        #region Properties
        [SerializeField] GeneralSubModifer m_GeneralSubModifier;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_GeneralSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            m_GeneralSubModifier.Save();
            base.OK();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifier.Initialize();
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">project preferences to display</param>
        protected override void SetFields(ProjectPreferences objectToDisplay)
        {
            m_GeneralSubModifier.Object = objectToDisplay;
        }
        #endregion
    }
}
