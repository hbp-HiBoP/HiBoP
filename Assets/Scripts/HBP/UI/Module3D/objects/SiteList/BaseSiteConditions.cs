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
    public abstract class BaseSiteConditions : MonoBehaviour
    {
        #region Properties
        protected Base3DScene m_Scene;
        private Queue<HBP.Module3D.Site> m_MatchingSites = new Queue<HBP.Module3D.Site>();
        private bool m_UpdateUI;
        #endregion

        #region Events
        public GenericEvent<float> OnFilter = new GenericEvent<float>();
        public GenericEvent<bool> OnEndFilter = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        protected abstract bool CheckConditions(HBP.Module3D.Site site);

        protected bool CheckHighlighted(HBP.Module3D.Site site)
        {
            return site.State.IsHighlighted;
        }
        protected bool CheckBlacklisted(HBP.Module3D.Site site)
        {
            return site.State.IsBlackListed;
        }
        protected bool CheckLabel(HBP.Module3D.Site site, string label)
        {
            return site.State.Labels.Any(l => l.ToLower().Contains(label.ToLower()));
        }
        protected bool CheckInROI(HBP.Module3D.Site site)
        {
            return !site.State.IsOutOfROI;
        }
        protected bool CheckInMesh(HBP.Module3D.Site site)
        {
            return m_Scene.MeshManager.SelectedMesh.SimplifiedBoth.IsPointInside(m_Scene.ImplantationManager.SelectedImplantation.RawSiteList, site.Information.GlobalID);
        }
        protected bool CheckInLeftHemisphere(HBP.Module3D.Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedLeft.IsPointInside(m_Scene.ImplantationManager.SelectedImplantation.RawSiteList, site.Information.GlobalID);
            }
            else
            {
                throw new InvalidBasicConditionException("The selected mesh is a single file mesh.\nYou can not filter by hemisphere.");
            }
        }
        protected bool CheckInRightHemisphere(HBP.Module3D.Site site)
        {
            if (m_Scene.MeshManager.SelectedMesh is LeftRightMesh3D mesh)
            {
                return mesh.SimplifiedRight.IsPointInside(m_Scene.ImplantationManager.SelectedImplantation.RawSiteList, site.Information.GlobalID);
            }
            else
            {
                throw new InvalidBasicConditionException("The selected mesh is a single file mesh.\nYou can not filter by hemisphere.");
            }
        }
        protected bool CheckOnPlane(HBP.Module3D.Site site)
        {
            return m_Scene.ImplantationManager.SelectedImplantation.RawSiteList.IsSiteOnAnyPlane(site, from cut in m_Scene.Cuts select cut as HBP.Module3D.Plane, 1.0f);
        }
        protected bool CheckName(HBP.Module3D.Site site, string name)
        {
            return site.Information.ChannelName.ToUpper().Contains(name.ToUpper());
        }
        protected bool CheckPatientName(HBP.Module3D.Site site, string patientName)
        {
            return site.Information.Patient.Name.ToUpper().Contains(patientName.ToUpper());
        }
        protected bool CheckPatientPlace(HBP.Module3D.Site site, string patientPlace)
        {
            return site.Information.Patient.Place.ToUpper().Contains(patientPlace.ToUpper());
        }
        protected bool CheckPatientDate(HBP.Module3D.Site site, string patientDateString)
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
        protected bool CheckMarsAtlasName(HBP.Module3D.Site site, string marsAtlasName)
        {
            return site.Information.MarsAtlasLabel.ToLower().Contains(marsAtlasName.ToLower());
        }
        protected bool CheckBroadmanAreaName(HBP.Module3D.Site site, string broadmanAreaName)
        {
            return site.Information.BrodmannAreaName.ToLower().Contains(broadmanAreaName.ToLower());
        }
        protected bool CheckFreesurferName(HBP.Module3D.Site site, string freesurferName)
        {
            return site.Information.FreesurferLabel.ToLower().Contains(freesurferName.ToLower());
        }
        protected bool CheckMean(HBP.Module3D.Site site, bool superior, string stringValue)
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
        protected bool CheckMedian(HBP.Module3D.Site site, bool superior, string stringValue)
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
        protected bool CheckMax(HBP.Module3D.Site site, bool superior, string stringValue)
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
        protected bool CheckMin(HBP.Module3D.Site site, bool superior, string stringValue)
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
        protected bool CheckStandardDeviation(HBP.Module3D.Site site, bool superior, string stringValue)
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
        private bool CompareValue(float value, bool superior, string stringValueToCompare)
        {
            if (global::Tools.CSharp.NumberExtension.TryParseFloat(stringValueToCompare, out float valueToCompare))
            {
                return superior ? value > valueToCompare : value < valueToCompare;
            }
            else
            {
                throw new ParsingValueException(stringValueToCompare);
            }
        }
        private void Update()
        {
            m_UpdateUI = true;
        }
        #endregion

        #region Public Methods
        public virtual void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
        }
        #endregion

        #region Coroutines
        public IEnumerator c_FilterSitesWithConditions(List<HBP.Module3D.Site> sites)
        {
            int length = sites.Count;
            Exception exception = null;
            for (int i = 0; i < length; ++i)
            {
                try
                {
                    HBP.Module3D.Site site = sites[i];
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
                        HBP.Module3D.Site filteredSite = m_MatchingSites.Dequeue();
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