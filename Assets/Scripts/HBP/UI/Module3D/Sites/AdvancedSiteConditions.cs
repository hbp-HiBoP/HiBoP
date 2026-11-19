using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Exceptions;
using HBP.Data.Tools;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.Core.Preferences;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Class used to define a set of conditions to check on the sites from a string
    /// </summary>
    public class AdvancedSiteConditions : BaseSiteConditions
    {
        #region Properties

        public const string TRUE = SiteConditions.TRUE;
        public const string FALSE = SiteConditions.FALSE;
        public const string HIGHLIGHTED = SiteConditions.HIGHLIGHTED;
        public const string BLACKLISTED = SiteConditions.BLACKLISTED;
        public const string LABEL = SiteConditions.LABEL;
        public const string IN_ROI = SiteConditions.IN_ROI;
        public const string IN_MESH = SiteConditions.IN_MESH;
        public const string IN_LEFT_HEMISPHERE = SiteConditions.IN_LEFT_HEMISPHERE;
        public const string IN_RIGHT_HEMISPHERE = SiteConditions.IN_RIGHT_HEMISPHERE;
        public const string ON_PLANE = SiteConditions.ON_PLANE;
        public const string ATLAS_AREA = SiteConditions.ATLAS_AREA;
        public const string POS_X = SiteConditions.POS_X;
        public const string POS_Y = SiteConditions.POS_Y;
        public const string POS_Z = SiteConditions.POS_Z;
        public const string NAME = SiteConditions.NAME;
        public const string PATIENT_NAME = SiteConditions.PATIENT_NAME;
        public const string TAG = SiteConditions.TAG;
        public const string MEAN = SiteConditions.MEAN;
        public const string MEDIAN = SiteConditions.MEDIAN;
        public const string MAX = SiteConditions.MAX;
        public const string MIN = SiteConditions.MIN;
        public const string STANDARD_DEVIATION = SiteConditions.STANDARD_DEVIATION;

        /// <summary>
        /// InputField used to write the string to be parsed as a set of conditions
        /// </summary>
        [SerializeField] InputField m_InputField;

        /// <summary>
        /// Boolean expression parsed from the string
        /// </summary>
        private System.Func<Core.Object3D.Site, bool> m_Filter;

        [SerializeField] AdvancedSiteConditionList m_AdvancedSiteConditionList;
        [SerializeField] Button m_StoreConditionButton;
        [SerializeField] Button m_ApplySelectedConditionButton;

        #endregion

        #region Private Methods

        private void Awake()
        {
            m_AdvancedSiteConditionList.Set(AdvancedSiteConditionStrings.Conditions);
            AdvancedSiteConditionStrings.OnChangeConditions.AddListener(() => { m_AdvancedSiteConditionList.Set(AdvancedSiteConditionStrings.Conditions); });
            m_StoreConditionButton.onClick.AddListener(StoreCondition);
            m_ApplySelectedConditionButton.onClick.AddListener(ApplySelectedCondition);
        }

        protected override System.Func<Core.Object3D.Site, bool> CreateFilter() => m_Filter;

        #endregion

        #region Public Methods

        /// <summary>
        /// Parse the whole string and store it to a BooleanExpression object
        /// </summary>
        public void ParseConditions()
        {
            m_Filter = Conditions.Parse(m_InputField.text);
        }

        public void StoreCondition()
        {
            AdvancedSiteConditionStrings.AddCondition(m_InputField.text);
        }

        public void ApplySelectedCondition()
        {
            if (m_AdvancedSiteConditionList.ObjectsSelected.Length > 0)
                m_InputField.text = m_AdvancedSiteConditionList.ObjectsSelected[0];
        }

        #endregion
    }
}
