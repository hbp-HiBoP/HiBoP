using HBP.Core.Data;
using HBP.Module3D;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Class used to define a set of conditions to check on the sites from the UI
    /// </summary>
    public class BasicSiteConditions : BaseSiteConditions
    {
        #region Properties
        // State
        [SerializeField] Toggle m_Highlighted;
        [SerializeField] Toggle m_NotHighlighted;
        [SerializeField] Toggle m_Blacklisted;
        [SerializeField] Toggle m_NotBlacklisted;
        [SerializeField] Toggle m_Label;
        [SerializeField] InputField m_LabelFilter;

        // Position
        [SerializeField] Toggle m_InROI;
        [SerializeField] Toggle m_OutOfROI;
        [SerializeField] Toggle m_InMesh;
        [SerializeField] Toggle m_OutOfMesh;
        [SerializeField] Toggle m_LeftHemisphere;
        [SerializeField] Toggle m_RightHemisphere;
        [SerializeField] Toggle m_OnPlane;
        [SerializeField] Toggle m_NotOnPlane;

        // Information
        [SerializeField] Toggle m_SiteName;
        [SerializeField] InputField m_SiteNameFilter;
        [SerializeField] Toggle m_Patient;
        [SerializeField] InputField m_PatientNameFilter;
        [SerializeField] InputField m_PatientPlaceFilter;
        [SerializeField] InputField m_PatientDateFilter;
        [SerializeField] Toggle m_Tag;
        [SerializeField] Dropdown m_TagDropdown;
        [SerializeField] InputField m_TagFilter;
        private Core.Data.BaseTag m_SelectedTag;

        // Values
        [SerializeField] Toggle m_Mean;
        [SerializeField] Toggle m_MeanSuperior;
        [SerializeField] InputField m_MeanValue;
        [SerializeField] Toggle m_Median;
        [SerializeField] Toggle m_MedianSuperior;
        [SerializeField] InputField m_MedianValue;
        [SerializeField] Toggle m_Min;
        [SerializeField] Toggle m_MinSuperior;
        [SerializeField] InputField m_MinValue;
        [SerializeField] Toggle m_Max;
        [SerializeField] Toggle m_MaxSuperior;
        [SerializeField] InputField m_MaxValue;
        [SerializeField] Toggle m_StandardDeviation;
        [SerializeField] Toggle m_StandardDeviationSuperior;
        [SerializeField] InputField m_StandardDeviationValue;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this object
        /// </summary>
        /// <param name="scene">Parent scene of this object</param>
        public override void Initialize(Base3DScene scene)
        {
            base.Initialize(scene);
            m_TagDropdown.options.Clear();
            foreach (var tag in ApplicationState.ProjectLoaded.Preferences.GeneralTags)
            {
                m_TagDropdown.options.Add(new Dropdown.OptionData(tag.Name));
            }
            foreach (var tag in ApplicationState.ProjectLoaded.Preferences.SitesTags)
            {
                m_TagDropdown.options.Add(new Dropdown.OptionData(tag.Name));
            }
            m_TagDropdown.onValueChanged.AddListener((selected) =>
            {
                if (selected < ApplicationState.ProjectLoaded.Preferences.GeneralTags.Count)
                {
                    m_SelectedTag = ApplicationState.ProjectLoaded.Preferences.GeneralTags[selected];
                }
                else
                {
                    m_SelectedTag = ApplicationState.ProjectLoaded.Preferences.SitesTags[selected - ApplicationState.ProjectLoaded.Preferences.GeneralTags.Count];
                }
            });
            m_SelectedTag = ApplicationState.ProjectLoaded.Preferences.GeneralTags.Count > 0 ? ApplicationState.ProjectLoaded.Preferences.GeneralTags[0] : ApplicationState.ProjectLoaded.Preferences.SitesTags.Count > 0 ? ApplicationState.ProjectLoaded.Preferences.SitesTags[0] : null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Check conditions on the state of the site
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if all conditions match</returns>
        private bool CheckState(Core.Object3D.Site site)
        {
            bool result = true;
            if (m_Highlighted.isOn) result &= CheckHighlighted(site);
            if (m_NotHighlighted.isOn) result &= !CheckHighlighted(site);
            if (m_Blacklisted.isOn) result &= CheckBlacklisted(site);
            if (m_NotBlacklisted.isOn) result &= !CheckBlacklisted(site);
            if (m_Label.isOn) result &= CheckLabel(site, m_LabelFilter.text);
            return result;
        }
        /// <summary>
        /// Check conditions on the position of the site
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if all conditions match</returns>
        private bool CheckPosition(Core.Object3D.Site site)
        {
            bool result = true;
            if (m_InROI.isOn) result &= CheckInROI(site);
            if (m_OutOfROI.isOn) result &= !CheckInROI(site);
            if (m_InMesh.isOn) result &= CheckInMesh(site);
            if (m_OutOfMesh.isOn) result &= !CheckInMesh(site);
            if (m_LeftHemisphere.isOn) result &= CheckInLeftHemisphere(site);
            if (m_RightHemisphere.isOn) result &= CheckInRightHemisphere(site);
            if (m_OnPlane.isOn) result &= CheckOnPlane(site);
            if (m_NotOnPlane.isOn) result &= !CheckOnPlane(site);
            return result;
        }
        /// <summary>
        /// Check conditions on the information of the site
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if all conditions match</returns>
        private bool CheckInformation(Core.Object3D.Site site)
        {
            bool result = true;
            if (m_SiteName.isOn) result &= CheckName(site, m_SiteNameFilter.text);
            if (m_Patient.isOn)
            {
                if (!string.IsNullOrEmpty(m_PatientNameFilter.text))
                {
                    result &= CheckPatientName(site, m_PatientNameFilter.text);
                }
                if (!string.IsNullOrEmpty(m_PatientPlaceFilter.text))
                {
                    result &= CheckPatientPlace(site, m_PatientPlaceFilter.text);
                }
                if (!string.IsNullOrEmpty(m_PatientDateFilter.text))
                {
                    result &= CheckPatientDate(site, m_PatientDateFilter.text);
                }
            }
            if (m_Tag.isOn) result &= CheckTag(site, m_SelectedTag, m_TagFilter.text);
            return result;
        }
        /// <summary>
        /// Check conditions on the values of the channel associated with the site
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if all conditions match</returns>
        private bool CheckValues(Core.Object3D.Site site)
        {
            bool result = true;
            if (m_Mean.isOn) result &= CheckMean(site, m_MeanSuperior.isOn, m_MeanValue.text);
            if (m_Median.isOn) result &= CheckMedian(site, m_MedianSuperior.isOn, m_MedianValue.text);
            if (m_Max.isOn) result &= CheckMax(site, m_MaxSuperior.isOn, m_MaxValue.text);
            if (m_Min.isOn) result &= CheckMin(site, m_MinSuperior.isOn, m_MinValue.text);
            if (m_StandardDeviation.isOn) result &= CheckStandardDeviation(site, m_StandardDeviationSuperior.isOn, m_StandardDeviationValue.text);
            return result;
        }
        /// <summary>
        /// Check all the set conditions for a specific site
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the conditions are met</returns>
        protected override bool CheckConditions(Core.Object3D.Site site)
        {
            return CheckState(site) && CheckPosition(site) && CheckInformation(site) && CheckValues(site);
        }
        #endregion
    }
}