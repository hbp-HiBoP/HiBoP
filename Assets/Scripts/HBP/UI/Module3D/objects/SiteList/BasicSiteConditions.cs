using CielaSpike;
using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class BasicSiteConditions : BaseSiteConditions
    {
        #region Properties
        // State
        [SerializeField] Toggle m_Highlighted;
        [SerializeField] Toggle m_NotHighlighted;
        [SerializeField] Toggle m_Blacklisted;
        [SerializeField] Toggle m_NotBlacklisted;

        // Position
        [SerializeField] Toggle m_InROI;
        [SerializeField] Toggle m_OutOfROI;
        [SerializeField] Toggle m_InMesh;
        [SerializeField] Toggle m_OutOfMesh;
        [SerializeField] Toggle m_OnPlane;
        [SerializeField] Toggle m_NotOnPlane;

        // Information
        [SerializeField] Toggle m_SiteName;
        [SerializeField] InputField m_SiteNameFilter;
        [SerializeField] Toggle m_Patient;
        [SerializeField] InputField m_PatientFilter;
        [SerializeField] Toggle m_MarsAtlas;
        [SerializeField] InputField m_MarsAtlasFilter;
        [SerializeField] Toggle m_Broadman;
        [SerializeField] InputField m_BroadmanFilter;
        [SerializeField] Toggle m_Freesurfer;
        [SerializeField] InputField m_FreesurferFilter;

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

        #region Private Methods
        private bool CheckState(Site site)
        {
            bool result = true;
            if (m_Highlighted.isOn) result &= CheckHighlighted(site);
            if (m_NotHighlighted.isOn) result &= !CheckHighlighted(site);
            if (m_Blacklisted.isOn) result &= CheckBlacklisted(site);
            if (m_NotBlacklisted.isOn) result &= !CheckBlacklisted(site);
            return result;
        }
        private bool CheckPosition(Site site)
        {
            bool result = true;
            if (m_InROI.isOn) result &= CheckInROI(site);
            if (m_OutOfROI.isOn) result &= !CheckInROI(site);
            if (m_InMesh.isOn) result &= CheckInMesh(site);
            if (m_OutOfMesh.isOn) result &= !CheckInMesh(site);
            if (m_OnPlane.isOn) result &= CheckOnPlane(site);
            if (m_NotOnPlane.isOn) result &= !CheckOnPlane(site);
            return result;
        }
        private bool CheckInformation(Site site)
        {
            bool result = true;
            if (m_SiteName.isOn) result &= CheckName(site, m_SiteNameFilter.text);
            if (m_Patient.isOn) result &= CheckPatientName(site, m_PatientFilter.text);
            if (m_MarsAtlas.isOn) result &= CheckMarsAtlasName(site, m_MarsAtlasFilter.text);
            if (m_Broadman.isOn) result &= CheckBroadmanAreaName(site, m_BroadmanFilter.text);
            if (m_Freesurfer.isOn) result &= CheckFreesurferName(site, m_FreesurferFilter.text);
            return result;
        }
        private bool CheckValues(Site site)
        {
            bool result = true;
            if (m_Mean.isOn) result &= CheckMean(site, m_MeanSuperior.isOn, m_MeanValue.text);
            if (m_Median.isOn) result &= CheckMedian(site, m_MedianSuperior.isOn, m_MedianValue.text);
            if (m_Max.isOn) result &= CheckMax(site, m_MaxSuperior.isOn, m_MaxValue.text);
            if (m_Min.isOn) result &= CheckMin(site, m_MinSuperior.isOn, m_MinValue.text);
            if (m_StandardDeviation.isOn) result &= CheckStandardDeviation(site, m_StandardDeviationSuperior.isOn, m_StandardDeviationValue.text);
            return result;
        }
        protected override bool CheckConditions(Site site)
        {
            return CheckState(site) && CheckPosition(site) && CheckInformation(site) && CheckValues(site);
        }
        #endregion
    }
}