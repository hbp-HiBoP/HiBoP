using HBP.Core.Data;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Preferences;

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
        [SerializeField] Toggle m_Atlas;
        [SerializeField] InputField m_AtlasFilter;
        [SerializeField] Toggle m_X;
        [SerializeField] Toggle m_XSuperior;
        [SerializeField] InputField m_XValue;
        [SerializeField] Toggle m_Y;
        [SerializeField] Toggle m_YSuperior;
        [SerializeField] InputField m_YValue;
        [SerializeField] Toggle m_Z;
        [SerializeField] Toggle m_ZSuperior;
        [SerializeField] InputField m_ZValue;

        // Information
        [SerializeField] Toggle m_SiteName;
        [SerializeField] InputField m_SiteNameFilter;
        [SerializeField] Toggle m_Patient;
        [SerializeField] InputField m_PatientNameFilter;
        [SerializeField] Toggle m_Tag;
        [SerializeField] Dropdown m_TagDropdown;
        [SerializeField] InputField m_TagFilter;
        private BaseTag m_SelectedTag;

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
            foreach (var tag in PersistentDataManager.Tags.GeneralTags)
            {
                m_TagDropdown.options.Add(new Dropdown.OptionData(tag.Name));
            }

            foreach (var tag in PersistentDataManager.Tags.SitesTags)
            {
                m_TagDropdown.options.Add(new Dropdown.OptionData(tag.Name));
            }

            m_TagDropdown.onValueChanged.AddListener((selected) =>
            {
                if (selected < PersistentDataManager.Tags.GeneralTags.Count)
                {
                    m_SelectedTag = PersistentDataManager.Tags.GeneralTags[selected];
                }
                else
                {
                    m_SelectedTag = PersistentDataManager.Tags.SitesTags[selected - PersistentDataManager.Tags.GeneralTags.Count];
                }
            });
            m_SelectedTag = PersistentDataManager.Tags.GeneralTags.Count > 0 ? PersistentDataManager.Tags.GeneralTags[0] : PersistentDataManager.Tags.SitesTags.Count > 0 ? PersistentDataManager.Tags.SitesTags[0] : null;
        }

        #endregion

        protected override System.Func<Core.Object3D.Site, bool> CreateFilter()
        {
            var Highlighted_isOn = m_Highlighted.isOn;
            var NotHighlighted_isOn = m_NotHighlighted.isOn;
            var Blacklisted_isOn = m_Blacklisted.isOn;
            var NotBlacklisted_isOn = m_NotBlacklisted.isOn;
            var Label_isOn = m_Label.isOn;
            var LabelFilter_text = m_LabelFilter.text;
            var InROI_isOn = m_InROI.isOn;
            var OutOfROI_isOn = m_OutOfROI.isOn;
            var InMesh_isOn = m_InMesh.isOn;
            var OutOfMesh_isOn = m_OutOfMesh.isOn;
            var LeftHemisphere_isOn = m_LeftHemisphere.isOn;
            var RightHemisphere_isOn = m_RightHemisphere.isOn;
            var OnPlane_isOn = m_OnPlane.isOn;
            var NotOnPlane_isOn = m_NotOnPlane.isOn;
            var Atlas_isOn = m_Atlas.isOn;
            var AtlasFilter_text = m_AtlasFilter.text;
            var X_isOn = m_X.isOn;
            var XSuperior_isOn = m_XSuperior.isOn;
            var XValue_text = m_XValue.text;
            var Y_isOn = m_Y.isOn;
            var YValue_text = m_YValue.text;
            var Z_isOn = m_Z.isOn;
            var ZValue_text = m_ZValue.text;
            var SiteName_isOn = m_SiteName.isOn;
            var SiteNameFilter_text = m_SiteNameFilter.text;
            var Patient_isOn = m_Patient.isOn;
            var PatientNameFilter_text = m_PatientNameFilter.text;
            var Tag_isOn = m_Tag.isOn;
            var TagFilter_text = m_TagFilter.text;
            var Mean_isOn = m_Mean.isOn;
            var MeanSuperior_isOn = m_MeanSuperior.isOn;
            var MeanValue_text = m_MeanValue.text;
            var Median_isOn = m_Median.isOn;
            var MedianSuperior_isOn = m_MedianSuperior.isOn;
            var MedianValue_text = m_MedianValue.text;
            var Max_isOn = m_Max.isOn;
            var MaxSuperior_isOn = m_MaxSuperior.isOn;
            var MaxValue_text = m_MaxValue.text;
            var Min_isOn = m_Min.isOn;
            var MinSuperior_isOn = m_MinSuperior.isOn;
            var MinValue_text = m_MinValue.text;
            var StandardDeviation_isOn = m_StandardDeviation.isOn;
            var StandardDeviationSuperior_isOn = m_StandardDeviationSuperior.isOn;
            var StandardDeviationValue_text = m_StandardDeviationValue.text;
            var selectedTag = m_SelectedTag;

            var YSuperior_isOn = m_YSuperior.isOn;
            var ZSuperior_isOn = m_ZSuperior.isOn;

            bool CheckState(Core.Object3D.Site site)
            {
                bool result = true;
                if (Highlighted_isOn) result &= Conditions.CheckHighlighted(site);
                if (NotHighlighted_isOn) result &= !Conditions.CheckHighlighted(site);
                if (Blacklisted_isOn) result &= Conditions.CheckBlacklisted(site);
                if (NotBlacklisted_isOn) result &= !Conditions.CheckBlacklisted(site);
                if (Label_isOn) result &= Conditions.CheckLabel(site, LabelFilter_text);
                return result;
            }

            bool CheckPosition(Core.Object3D.Site site)
            {
                bool result = true;
                if (InROI_isOn) result &= Conditions.CheckInROI(site);
                if (OutOfROI_isOn) result &= !Conditions.CheckInROI(site);
                if (InMesh_isOn) result &= Conditions.CheckInMesh(site);
                if (OutOfMesh_isOn) result &= !Conditions.CheckInMesh(site);
                if (LeftHemisphere_isOn) result &= Conditions.CheckInLeftHemisphere(site);
                if (RightHemisphere_isOn) result &= Conditions.CheckInRightHemisphere(site);
                if (OnPlane_isOn) result &= Conditions.CheckOnPlane(site);
                if (NotOnPlane_isOn) result &= !Conditions.CheckOnPlane(site);
                if (Atlas_isOn) result &= Conditions.CheckAtlas(site, AtlasFilter_text);
                if (X_isOn) result &= Conditions.CheckX(site, XSuperior_isOn, XValue_text);
                if (Y_isOn) result &= Conditions.CheckY(site, YSuperior_isOn, YValue_text);
                if (Z_isOn) result &= Conditions.CheckZ(site, ZSuperior_isOn, ZValue_text);
                return result;
            }

            bool CheckInformation(Core.Object3D.Site site)
            {
                bool result = true;
                if (SiteName_isOn) result &= Conditions.CheckName(site, SiteNameFilter_text);
                if (Patient_isOn)
                {
                    if (!string.IsNullOrEmpty(PatientNameFilter_text))
                    {
                        result &= Conditions.CheckPatientName(site, PatientNameFilter_text);
                    }
                }

                if (Tag_isOn) result &= Conditions.CheckTag(site, selectedTag, TagFilter_text);
                return result;
            }

            bool CheckValues(Core.Object3D.Site site)
            {
                bool result = true;
                if (Mean_isOn) result &= Conditions.CheckMean(site, MeanSuperior_isOn, MeanValue_text);
                if (Median_isOn) result &= Conditions.CheckMedian(site, MedianSuperior_isOn, MedianValue_text);
                if (Max_isOn) result &= Conditions.CheckMax(site, MaxSuperior_isOn, MaxValue_text);
                if (Min_isOn) result &= Conditions.CheckMin(site, MinSuperior_isOn, MinValue_text);
                if (StandardDeviation_isOn) result &= Conditions.CheckStandardDeviation(site, StandardDeviationSuperior_isOn, StandardDeviationValue_text);
                return result;
            }

            return site => CheckState(site) && CheckPosition(site) && CheckInformation(site) && CheckValues(site);
        }
    }
}
