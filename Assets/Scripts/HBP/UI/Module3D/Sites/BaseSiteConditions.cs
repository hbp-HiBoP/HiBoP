using ThirdParty.CielaSpike;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Exceptions;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.DLL;
using HBP.Core.Tools;

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
        private Queue<Core.Object3D.Site> m_MatchingSites = new Queue<Core.Object3D.Site>();
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
        protected abstract bool CheckConditions(Core.Object3D.Site site);
        /// <summary>
        /// Check if the site is highlighted
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is highlighted</returns>
        protected bool CheckHighlighted(Core.Object3D.Site site)
        {
            return site.State.IsHighlighted;
        }
        /// <summary>
        /// Check if the site is blacklisted
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is blacklisted</returns>
        protected bool CheckBlacklisted(Core.Object3D.Site site)
        {
            return site.State.IsBlackListed;
        }
        /// <summary>
        /// Check if the site has a label which contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="label">Label to use for the checking</param>
        /// <returns>True if the site has a label which contains the input string</returns>
        protected bool CheckLabel(Core.Object3D.Site site, string label)
        {
            return site.State.Labels.Any(l => l.ToLower().Contains(label.ToLower()));
        }
        /// <summary>
        /// Check if the site is in the currently selected ROI
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the currently selected ROI</returns>
        protected bool CheckInROI(Core.Object3D.Site site)
        {
            return !site.State.IsOutOfROI;
        }
        /// <summary>
        /// Check if the site is in the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the currently selected mesh</returns>
        protected bool CheckInMesh(Core.Object3D.Site site)
        {
            return m_Scene.MeshManager.SelectedMesh.SimplifiedBoth.IsPointInside(site.Information.DefaultPosition);
        }
        /// <summary>
        /// Check if the site is in the left hemisphere of the currently selected mesh
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <returns>True if the site is in the left hemisphere of the currently selected mesh</returns>
        protected bool CheckInLeftHemisphere(Core.Object3D.Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedLeft.IsPointInside(site.Information.DefaultPosition);
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
        protected bool CheckInRightHemisphere(Core.Object3D.Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is Core.Object3D.LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedRight.IsPointInside(site.Information.DefaultPosition);
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
        protected bool CheckOnPlane(Core.Object3D.Site site)
        {
            return m_Scene.ImplantationManager.SelectedImplantation.RawSiteList.IsSiteOnAnyPlane(site, from cut in m_Scene.Cuts select cut as Core.Object3D.Plane, 1.0f);
        }
        /// <summary>
        /// Check if the site is in a specific area of the selected atlas
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="areaName">Name to use for the checking</param>
        /// <returns>True if the site is in the specified area of the selected atlas</returns>
        protected bool CheckAtlas(Core.Object3D.Site site, string areaName)
        {
            if (m_Scene.AtlasManager.SelectedAtlas != null)
            {
                int areaID = m_Scene.AtlasManager.SelectedAtlas.GetClosestAreaIndex(site.Information.DefaultPosition);
                if (int.TryParse(areaName, out int comparedID))
                {
                    if (areaID == comparedID) return true;
                }
                string[] areaInformation = m_Scene.AtlasManager.SelectedAtlas.GetInformation(areaID);
                if (areaInformation.Length == 5)
                {
                    if (m_Scene.AtlasManager.SelectedAtlas is MarsAtlas)
                    {
                        // Check in name
                        if (areaInformation[0].ToUpper().Contains(areaName.ToUpper())) return true;
                        // Check in full name
                        if (areaInformation[4].ToUpper().Contains(areaName.ToUpper())) return true;
                    }
                    else
                    {
                        // Check in region
                        if (areaInformation[0].ToUpper().Contains(areaName.ToUpper())) return true;
                        // Check in location
                        if (areaInformation[1].ToUpper().Contains(areaName.ToUpper())) return true;
                        // Check in area label
                        if (areaInformation[2].ToUpper().Contains(areaName.ToUpper())) return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Check if the site name contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="name">Name to use for the checking</param>
        /// <returns>True if the site name contains the input string</returns>
        protected bool CheckName(Core.Object3D.Site site, string name)
        {
            return site.Information.Name.ToUpper().Contains(name.ToUpper());
        }
        /// <summary>
        /// Check if the site patient name contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="areaName">Name to use for the checking</param>
        /// <returns>True if the site patient name contains the input string</returns>
        protected bool CheckPatientName(Core.Object3D.Site site, string patientName)
        {
            return site.Information.Patient.Name.ToUpper().Contains(patientName.ToUpper());
        }
        /// <summary>
        /// Check if the site patient place contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="patientPlace">Place to use for the checking</param>
        /// <returns>True if the site patient place contains the input string</returns>
        protected bool CheckPatientPlace(Core.Object3D.Site site, string patientPlace)
        {
            return site.Information.Patient.Place.ToUpper().Contains(patientPlace.ToUpper());
        }
        /// <summary>
        /// Check if the site patient date contains the input string
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="patientDateString">Date to use for the checking</param>
        /// <returns>True if the site patient date contains the input string</returns>
        protected bool CheckPatientDate(Core.Object3D.Site site, string patientDateString)
        {
            if (NumberExtension.TryParseFloat(patientDateString, out float patientDate))
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
        protected bool CheckTag(Core.Object3D.Site site, Core.Data.BaseTag tag, string tagValueToCompare)
        {
            if (tag != null)
            {
                Core.Data.BaseTagValue tagValue = site.Information.SiteData.Tags.FirstOrDefault(t => t.Tag == tag);
                if (tagValue != null)
                {
                    return tagValue.DisplayableValue.ToUpper().Contains(tagValueToCompare.ToUpper());
                }
                else return false;
            }
            else return false;
        }
        // TODO : Consider start and end for all following methods
        /// <summary>
        /// Check if the mean of the values of the associated channel if above or under the input value
        /// </summary>
        /// <param name="site">Site to check</param>
        /// <param name="superior">True if the condition is "above", false otherwise</param>
        /// <param name="stringValue">Value to be compared</param>
        /// <returns>True if the mean of the values matches the condition</returns>
        protected bool CheckMean(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
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
        protected bool CheckMedian(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
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
        protected bool CheckMax(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
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
        protected bool CheckMin(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
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
        protected bool CheckStandardDeviation(Core.Object3D.Site site, bool superior, string stringValue, int start = 0, int end = 0)
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
        public IEnumerator c_FilterSitesWithConditions(List<Core.Object3D.Site> sites)
        {
            int length = sites.Count;
            Exception exception = null;
            for (int i = 0; i < length; ++i)
            {
                try
                {
                    Core.Object3D.Site site = sites[i];
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
                        Core.Object3D.Site filteredSite = m_MatchingSites.Dequeue();
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
                DialogBoxManager.Open(DialogBoxManager.AlertType.Warning, exception.ToString(), exception.Message);
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