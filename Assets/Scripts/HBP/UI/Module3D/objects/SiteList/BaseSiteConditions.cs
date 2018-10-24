using CielaSpike;
using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

public abstract class BaseSiteConditions : MonoBehaviour
{
    #region Properties
    protected Base3DScene m_Scene;
    private Queue<Site> m_MatchingSites = new Queue<Site>();
    private bool m_UpdateUI;
    public GenericEvent<Site> OnApplyActionOnSite = new GenericEvent<Site>();
    #endregion

    #region Private Methods
    protected abstract bool CheckConditions(Site site);
    protected bool CheckExcluded(Site site)
    {
        return site.State.IsExcluded;
    }
    protected bool CheckHighlighted(Site site)
    {
        return site.State.IsHighlighted;
    }
    protected bool CheckBlacklisted(Site site)
    {
        return site.State.IsBlackListed;
    }
    protected bool CheckMarked(Site site)
    {
        return site.State.IsMarked;
    }
    protected bool CheckSuspicious(Site site)
    {
        return site.State.IsSuspicious;
    }
    protected bool CheckInROI(Site site)
    {
        return !site.State.IsOutOfROI;
    }
    protected bool CheckInMesh(Site site)
    {
        return m_Scene.ColumnManager.SelectedMesh.SimplifiedBoth.IsPointInside(m_Scene.ColumnManager.SelectedImplantation.RawSiteList, site.Information.GlobalID);
    }
    protected bool CheckOnPlane(Site site)
    {
        return m_Scene.ColumnManager.SelectedImplantation.RawSiteList.IsSiteOnAnyPlane(site, from cut in m_Scene.Cuts select cut as HBP.Module3D.Plane, 1.0f);
    }
    protected bool CheckName(Site site, string name)
    {
        return site.Information.ChannelName.ToUpper().Contains(name.ToUpper());
    }
    protected bool CheckPatientName(Site site, string patientName)
    {
        return site.Information.Patient.Name.ToUpper().Contains(patientName.ToUpper());
    }
    protected bool CheckMarsAtlasName(Site site, string marsAtlasName)
    {
        return ApplicationState.Module3D.MarsAtlasIndex.FullName(site.Information.MarsAtlasIndex).ToLower().Contains(marsAtlasName.ToLower());
    }
    protected bool CheckBroadmanAreaName(Site site, string broadmanAreaName)
    {
        return ApplicationState.Module3D.MarsAtlasIndex.BroadmanArea(site.Information.MarsAtlasIndex).ToLower().Contains(broadmanAreaName.ToLower());
    }
    protected bool CheckMean(Site site, bool superior, string stringValue)
    {
        float[] allValues = site.Statistics.Trial.AllValues;
        if (allValues.Length > 0)
        {
            return CompareValue(allValues.Mean(), superior, stringValue);
        }
        else
        {
            return false;
        }
    }
    protected bool CheckMedian(Site site, bool superior, string stringValue)
    {
        float[] allValues = site.Statistics.Trial.AllValues;
        if (allValues.Length > 0)
        {
            return CompareValue(allValues.Median(), superior, stringValue);
        }
        else
        {
            return false;
        }
    }
    protected bool CheckMax(Site site, bool superior, string stringValue)
    {
        float[] allValues = site.Statistics.Trial.AllValues;
        if (allValues.Length > 0)
        {
            return CompareValue(allValues.Max(), superior, stringValue);
        }
        else
        {
            return false;
        }
    }
    protected bool CheckMin(Site site, bool superior, string stringValue)
    {
        float[] allValues = site.Statistics.Trial.AllValues;
        if (allValues.Length > 0)
        {
            return CompareValue(allValues.Min(), superior, stringValue);
        }
        else
        {
            return false;
        }
    }
    protected bool CheckStandardDeviation(Site site, bool superior, string stringValue)
    {
        float[] allValues = site.Statistics.Trial.AllValues;
        if (allValues.Length > 0)
        {
            return CompareValue(allValues.StandardDeviation(), superior, stringValue);
        }
        else
        {
            return false;
        }
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
    public IEnumerator c_FindSites(List<Site> sites, UnityEvent onBegin, UnityEvent onEnd, UnityEvent<float> onProgress)
    {
        yield return Ninja.JumpToUnity;
        onBegin.Invoke();
        yield return Ninja.JumpBack;
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
                    Site siteToApply = m_MatchingSites.Dequeue();
                    OnApplyActionOnSite.Invoke(siteToApply);
                }
                onProgress.Invoke((float)(i + 1) / length);
                m_UpdateUI = false;
                yield return Ninja.JumpBack;
            }
        }
        yield return Ninja.JumpToUnity;
        if (exception != null)
        {
            ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Warning, exception.ToString(), exception.Message);
        }
        onEnd.Invoke();
    }
    #endregion
}
