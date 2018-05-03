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
    public class BasicSiteConditions : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;
        public GenericEvent<Site> OnApplyActionOnSite = new GenericEvent<Site>();

        // State
        [SerializeField] Toggle m_Excluded;
        [SerializeField] Toggle m_NotExcluded;
        [SerializeField] Toggle m_Highlighted;
        [SerializeField] Toggle m_NotHighlighted;
        [SerializeField] Toggle m_Blacklisted;
        [SerializeField] Toggle m_NotBlacklisted;
        [SerializeField] Toggle m_Marked;
        [SerializeField] Toggle m_NotMarked;
        [SerializeField] Toggle m_Suspicious;
        [SerializeField] Toggle m_NotSuspicious;

        // Position
        [SerializeField] Toggle m_InROI;
        [SerializeField] Toggle m_OutOfROI;
        [SerializeField] Toggle m_InMesh;
        [SerializeField] Toggle m_OutOfMesh;

        // Information
        [SerializeField] Toggle m_SiteName;
        [SerializeField] InputField m_SiteNameFilter;
        [SerializeField] Toggle m_Patient;
        [SerializeField] InputField m_PatientFilter;
        [SerializeField] Toggle m_MarsAtlas;
        [SerializeField] InputField m_MarsAtlasFilter;
        [SerializeField] Toggle m_Broadman;
        [SerializeField] InputField m_BroadmanFilter;

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
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
        }
        #endregion

        #region Private Methods
        private bool CheckState(Site site)
        {
            bool result = true;
            if (m_Excluded.isOn) result &= site.State.IsExcluded;
            if (m_NotExcluded.isOn) result &= !site.State.IsExcluded;
            if (m_Highlighted.isOn) result &= site.State.IsHighlighted;
            if (m_NotHighlighted.isOn) result &= !site.State.IsHighlighted;
            if (m_Blacklisted.isOn) result &= site.State.IsBlackListed;
            if (m_NotBlacklisted.isOn) result &= !site.State.IsBlackListed;
            if (m_Marked.isOn) result &= site.State.IsMarked;
            if (m_NotMarked.isOn) result &= !site.State.IsMarked;
            if (m_Suspicious.isOn) result &= site.State.IsSuspicious;
            if (m_NotSuspicious.isOn) result &= !site.State.IsSuspicious;
            return result;
        }
        private bool CheckPosition(Site site)
        {
            bool result = true;
            if (m_InROI.isOn) result &= !site.State.IsOutOfROI;
            if (m_OutOfROI.isOn) result &= site.State.IsOutOfROI;
            if (m_InMesh.isOn) result &= true; // FIXME
            if (m_OutOfMesh.isOn) result &= true; // FIXME
            return result;
        }
        private bool CheckInformation(Site site)
        {
            bool result = true;
            if (m_SiteName.isOn) result &= site.Information.ChannelName.ToUpper().Contains(m_SiteNameFilter.text.ToUpper());
            if (m_Patient.isOn) result &= site.Information.Patient.Name.ToUpper().Contains(m_PatientFilter.text.ToUpper());
            if (m_MarsAtlas.isOn) result &= ApplicationState.Module3D.MarsAtlasIndex.FullName(site.Information.MarsAtlasIndex).ToLower().Contains(m_MarsAtlasFilter.text.ToLower());
            if (m_Broadman.isOn) result &= ApplicationState.Module3D.MarsAtlasIndex.BroadmanArea(site.Information.MarsAtlasIndex).ToLower().Contains(m_BroadmanFilter.text.ToLower());
            return result;
        }
        private bool CheckValues(Site site)
        {
            bool result = true;
            if (site.Configuration.NormalizedValues.Length > 0)
            {
                if (m_Mean.isOn) result &= CompareValue(site.Configuration.NormalizedValues.Mean(), m_MeanSuperior.isOn, m_MeanValue.text);
                if (m_Median.isOn) result &= CompareValue(site.Configuration.NormalizedValues.Median(), m_MedianSuperior.isOn, m_MedianValue.text);
                if (m_Max.isOn) result &= CompareValue(site.Configuration.NormalizedValues.Max(), m_MaxSuperior.isOn, m_MaxValue.text);
                if (m_Min.isOn) result &= CompareValue(site.Configuration.NormalizedValues.Min(), m_MinSuperior.isOn, m_MinValue.text);
                if (m_StandardDeviation.isOn) result &= CompareValue(site.Configuration.NormalizedValues.StandardDeviation(), m_StandardDeviationSuperior.isOn, m_StandardDeviationValue.text);
            }
            else
            {
                result = false;
            }
            return result;
        }
        private bool CompareValue(float value, bool superior, string stringValueToCompare)
        {
            float valueToCompare = 0f;
            if (float.TryParse(stringValueToCompare, out valueToCompare))
            {
                return superior ? value > valueToCompare : value < valueToCompare;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Coroutines
        public IEnumerator c_FindSites(List<Site> sites, UnityEvent onBegin, UnityEvent onEnd, UnityEvent<float> onProgress)
        {
            yield return Ninja.JumpToUnity;
            onBegin.Invoke();
            int length = sites.Count;
            for (int i = 0; i < length; ++i)
            {
                Site site = sites[i];
                yield return Ninja.JumpBack;
                bool match = CheckState(site) && CheckPosition(site) && CheckInformation(site) && CheckValues(site);
                yield return Ninja.JumpToUnity;
                if (match)
                {
                    OnApplyActionOnSite.Invoke(site);
                }
                onProgress.Invoke((float)(i + 1) / length);
            }
            onEnd.Invoke();
        }
        #endregion
    }
}