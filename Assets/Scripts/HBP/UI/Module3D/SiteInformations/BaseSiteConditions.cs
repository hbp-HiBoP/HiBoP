using CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Base abstract class for the processing of the filtering of the sites depending on set conditions
    /// </summary>
    public abstract class BaseSiteConditions : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Associated Base3DScene
        /// </summary>
        protected Base3DScene m_Scene;
        /// <summary>
        /// Current queue of all sites that match the conditions (used to smooth the checking)
        /// </summary>
        private Queue<Site> m_MatchingSites = new Queue<Site>();
        /// <summary>
        /// Do we update the UI ?
        /// </summary>
        private bool m_UpdateUI;
        #endregion

        #region Events
        /// <summary>
        /// Event called when updating the filtering progress
        /// </summary>
        public GenericEvent<float> OnFilter = new GenericEvent<float>();
        /// <summary>
        /// Event called when the filtering is finished
        /// </summary>
        public GenericEvent<bool> OnEndFilter = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        private void Update()
        {
            m_UpdateUI = true;
        }
        /// <summary>
        /// Check all the set conditions for a specific site
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the conditions are met</returns>
        protected abstract bool CheckConditions(Site site);
        /// <summary>
        /// Check if the site is highlighted
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is highlighted</returns>
        protected bool CheckHighlighted(Site site)
        {
            return site.State.IsHighlighted;
        }
        /// <summary>
        /// Check if the site is blacklisted
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is blacklisted</returns>
        protected bool CheckBlacklisted(Site site)
        {
            return site.State.IsBlackListed;
        }
        /// <summary>
        /// Check if the site has a label which contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="label">Label to use for the checking</param>
        /// <returns>True if the site has a label which contains the input string</returns>
        protected bool CheckLabel(Site site, string label)
        {
            return site.State.Labels.Any(l => l.ToLower().Contains(label.ToLower()));
        }
        /// <summary>
        /// Check if the site is in the currently selected ROI
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the currently selected ROI</returns>
        protected bool CheckInROI(Site site)
        {
            return !site.State.IsOutOfROI;
        }
        /// <summary>
        /// Check if the site is in the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the currently selected mesh</returns>
        protected bool CheckInMesh(Site site)
        {
            return m_Scene.MeshManager.SelectedMesh.SimplifiedBoth.IsSiteInside(m_Scene.ImplantationManager.SelectedImplantation.RawSiteList, site.Information.Index);
        }
        /// <summary>
        /// Check if the site is in the left hemisphere of the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the left hemisphere of the currently selected mesh</returns>
        protected bool CheckInLeftHemisphere(Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedLeft.IsSiteInside(m_Scene.ImplantationManager.SelectedImplantation.RawSiteList, site.Information.Index);
            }
            else
            {
                throw new InvalidBasicConditionException("The selected mesh is a single file mesh.\nYou can not filter by hemisphere.");
            }
        }
        /// <summary>
        /// Check if the site is in the right hemisphere of the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the right hemisphere of the currently selected mesh</returns>
        protected bool CheckInRightHemisphere(Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedRight.IsSiteInside(m_Scene.ImplantationManager.SelectedImplantation.RawSiteList, site.Information.Index);
            }
            else
            {
                throw new InvalidBasicConditionException("The selected mesh is a single file mesh.\nYou can not filter by hemisphere.");
            }
        }
        /// <summary>
        /// Check if the site is on any cut plane of the scene
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is on any cut plane of the scene</returns>
        protected bool CheckOnPlane(Site site)
        {
            return m_Scene.ImplantationManager.SelectedImplantation.RawSiteList.IsSiteOnAnyPlane(site, from cut in m_Scene.Cuts select cut as HBP.Module3D.Plane, 1.0f);
        }
        /// <summary>
        /// Check if the site name contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="name">Name to use for the checking</param>
        /// <returns>True if the site name contains the input string</returns>
        protected bool CheckName(Site site, string name)
        {
            return site.Information.Name.ToUpper().Contains(name.ToUpper());
        }
        /// <summary>
        /// Check if the site patient name contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="patientName">Name to use for the checking</param>
        /// <returns>True if the site patient name contains the input string</returns>
        protected bool CheckPatientName(Site site, string patientName)
        {
            return site.Information.Patient.Name.ToUpper().Contains(patientName.ToUpper());
        }
        /// <summary>
        /// Check if the site patient place contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="patientPlace">Place to use for the checking</param>
        /// <returns>True if the site patient place contains the input string</returns>
        protected bool CheckPatientPlace(Site site, string patientPlace)
        {
            return site.Information.Patient.Place.ToUpper().Contains(patientPlace.ToUpper());
        }
        /// <summary>
        /// Check if the site patient date contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="patientDateString">Date to use for the checking</param>
        /// <returns>True if the site patient date contains the input string</returns>
        protected bool CheckPatientDate(Site site, string patientDateString)
        {
            if (global::Tools.CSharp.NumberExtension.TryParseFloat(patientDateString, out float patientDate))
            {
                return site.Information.Patient.Date == patientDate;
            }
            else
            {
                throw new ParsingValueException(patientDateString);
            }
        }
        /// <summary>
        /// Check if the site has the input tag and if the value of this tag contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="tag">Tag to be checked</param>
        /// <param name="tagValueToCompare">Value to be compared to the value of the tag</param>
        /// <returns></returns>
        protected bool CheckTag(Site site, Data.BaseTag tag, string tagValueToCompare)
        {
            if (tag != null)
            {
                Data.BaseTagValue tagValue = site.Information.SiteData.Tags.FirstOrDefault(t => t.Tag == tag);
                if (tagValue != null)
                {
                    return tagValue.DisplayableValue.ToUpper().Contains(tagValueToCompare.ToUpper());
                }
                else return false;
            }
            else return false;
        }
        /// <summary>
        /// Check if the mean of the values of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the mean of the values matches the condition</returns>
        protected bool CheckMean(Site site, bool superior, string stringValue)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Mean(), superior, stringValue);
                }
            }
            return false;
        }
        /// <summary>
        /// Check if the median of the values of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the median of the values matches the condition</returns>
        protected bool CheckMedian(Site site, bool superior, string stringValue)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Median(), superior, stringValue);
                }
            }
            return false;
        }
        /// <summary>
        /// Check if the maximum value of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the maximum value matches the condition</returns>
        protected bool CheckMax(Site site, bool superior, string stringValue)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Max(), superior, stringValue);
                }
            }
            return false;
        }
        /// <summary>
        /// Check if the minimum value of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the minimum value matches the condition</returns>
        protected bool CheckMin(Site site, bool superior, string stringValue)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.Min(), superior, stringValue);
                }
            }
            return false;
        }
        /// <summary>
        /// Check if the standard deviation of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the standard deviation matches the condition</returns>
        protected bool CheckStandardDeviation(Site site, bool superior, string stringValue)
        {
            if (site.Statistics != null)
            {
                float[] allValues = site.Statistics.Trial.AllValues;
                if (allValues.Length > 0)
                {
                    return CompareValue(allValues.StandardDeviation(), superior, stringValue);
                }
            }
            return false;
        }
        /// <summary>
        /// Compare two values (one is a float, the other is a parsable to float string)
        /// </summary>
        /// <param name="value">Float value to compare</param>
        /// <param name="superior">True if the comparison is "greater than", false otherwise</param>
        /// <param name="stringValueToCompare">String value to compare</param>
        /// <returns>True if the float value matches the condition</returns>
        private bool CompareValue(float value, bool superior, string stringValueToCompare)
        {
            if (NumberExtension.TryParseFloat(stringValueToCompare, out float valueToCompare))
            {
                return superior ? value > valueToCompare : value < valueToCompare;
            }
            else
            {
                throw new ParsingValueException(stringValueToCompare);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this object
        /// </summary>
        /// <param name="scene">Parent scene of this object</param>
        public virtual void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Coroutine used to check the conditions in asynchronous mode
        /// </summary>
        /// <param name="sites">List of the sites to be checked using the set of conditions defined in the subclasses</param>
        /// <returns>Coroutine return</returns>
        public IEnumerator c_FilterSitesWithConditions(List<Site> sites)
        {
            int length = sites.Count;
            Exception exception = null;
            for (int i = 0; i < length; ++i)
            {
                try
                {
                    Site site = sites[i];
                    bool match = CheckConditions(site);
                    if (match)
                    {
                        m_MatchingSites.Enqueue(site);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    exception = e;
                }
                if (exception != null)
                {
                    break;
                }
                if (m_UpdateUI || i == length - 1)
                {
                    yield return Ninja.JumpToUnity;
                    while (m_MatchingSites.Count > 0)
                    {
                        Site filteredSite = m_MatchingSites.Dequeue();
                        filteredSite.State.IsFiltered = true;
                    }
                    OnFilter.Invoke((float)(i + 1) / length);
                    m_UpdateUI = false;
                    yield return Ninja.JumpBack;
                }
            }
            yield return Ninja.JumpToUnity;
            if (exception != null)
            {
                ApplicationState.DialogBoxManager.Open(global::Tools.Unity.DialogBoxManager.AlertType.Warning, exception.ToString(), exception.Message);
                OnEndFilter.Invoke(false);
            }
            else
            {
                OnEndFilter.Invoke(true);
            }
        }
        #endregion
    }
}