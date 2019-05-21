



// unity
using HBP.Data.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
/**
* \file    Site.cs
* \author  Lance Florian
* \date    2015
* \brief   Define site related classes
*/
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    public class SiteInformation
    {
        public SiteInformation(SiteInformation info)
        {
            Patient = info.Patient;
            Name = info.Name;
            SitePatientID = info.SitePatientID;
            PatientNumber = info.PatientNumber;
            ElectrodeNumber = info.ElectrodeNumber;
            SiteNumber = info.SiteNumber;
            MarsAtlasIndex = info.MarsAtlasIndex;
        }
        public SiteInformation() { }

        public Data.Patient Patient { get; set; }
        public string Name { get; set; }

        public int GlobalID { get; set; }
        public int SitePatientID { get; set; }

        public int PatientNumber { get; set; }
        public int ElectrodeNumber { get; set; }
        public int SiteNumber { get; set; }

        private int m_MarsAtlasIndex;
        public int MarsAtlasIndex
        {
            get
            {
                return m_MarsAtlasIndex;
            }
            set
            {
                m_MarsAtlasIndex = value;
                MarsAtlasLabel = string.Format("{0}/{1}/{2}/{3}",
                    ApplicationState.Module3D.MarsAtlasIndex.Hemisphere(value).Replace('_', ' '),
                    ApplicationState.Module3D.MarsAtlasIndex.Lobe(value).Replace('_', ' '),
                    ApplicationState.Module3D.MarsAtlasIndex.NameFS(value).Replace('_', ' '),
                    ApplicationState.Module3D.MarsAtlasIndex.FullName(value).Replace('_', ' '));
                BroadmanAreaName = ApplicationState.Module3D.MarsAtlasIndex.BroadmanArea(value).Replace('_', ' ');
            }
        }
        public string MarsAtlasLabel { get; private set; }
        public string BroadmanAreaName { get; private set; }
        public string FreesurferLabel { get; set; }

        public string PatientID
        {
            get
            {
                return Patient.ID;
            }
        }
        public string FullID
        {
            get
            {
                return PatientID + "_" + Name;
            }
        }
        public string FullCorrectedID
        {
            get
            {
                if (ApplicationState.UserPreferences.Data.Anatomic.SiteNameCorrection)
                {
                    string siteName = Name.ToUpper();
                    int prime = siteName.LastIndexOf('P');
                    if (prime > 0)
                    {
                        return PatientID + "_" + siteName.Remove(prime, 1).Insert(prime, "\'");
                    }
                    else
                    {
                        return PatientID + "_" + siteName;
                    }
                }
                else
                {
                    return FullID;
                }
            }
        }
        public string DisplayedName
        {
            get
            {
                return ChannelName + " (" + Patient.Name + " - " + Patient.Place + " - " + Patient.Date + ")";
            }
        }
        public string ChannelName
        {
            get { return FullCorrectedID.Replace(PatientID + "_", ""); }
        }
    }

    public class SiteState
    {
        public static Color DefaultColor = new Color(0.53f, 0.15f, 0.15f);
        public UnityEvent OnChangeState = new UnityEvent();
        public SiteState(SiteState state)
        {
            ApplyState(state);
        }
        public SiteState() { }
        public bool IsMasked { get; set; }
        public bool IsOutOfROI { get; set; }
        private bool m_IsBlackListed;
        public bool IsBlackListed
        {
            get
            {
                return m_IsBlackListed;
            }
            set
            {
                m_IsBlackListed = value;
                OnChangeState.Invoke();
            }
        }
        private bool m_IsHighlighted;
        public bool IsHighlighted
        {
            get
            {
                return m_IsHighlighted;
            }
            set
            {
                m_IsHighlighted = value;
                OnChangeState.Invoke();
            }
        }
        private Color m_Color = DefaultColor;
        public Color Color
        {
            get
            {
                return m_Color;
            }
            set
            {
                m_Color = value;
                OnChangeState.Invoke();
            }
        }
        public List<string> Labels { get; set; } = new List<string>();
        public void AddLabel(string label)
        {
            if (!Labels.Contains(label))
            {
                Labels.Add(label);
                OnChangeState.Invoke();
            }
        }
        public void RemoveLabel(string label)
        {
            Labels.Remove(label);
            OnChangeState.Invoke();
        }
        public void ApplyState(SiteState state)
        {
            ApplyState(state.IsBlackListed, state.IsHighlighted, state.Color, state.Labels);
        }
        public void ApplyState(bool blacklisted, bool highlighted, Color color, IEnumerable<string> labels)
        {
            m_IsBlackListed = blacklisted;
            m_IsHighlighted = highlighted;
            m_Color = color;
            Labels = labels.ToList();
            OnChangeState.Invoke();
        }
    }

    /// <summary>
    /// Class for defining informations about a plot
    /// </summary>
    public class Site : MonoBehaviour, IConfigurable
    {
        #region Properties
        /// <summary>
        /// Has this site been initialized ?
        /// </summary>
        private bool m_IsInitialized = false;

        bool m_IsActive;
        /// <summary>
        /// Is this site active ?
        /// </summary>
        public bool IsActive
        {
            get
            {
                if (!m_IsInitialized)
                {
                    m_IsActive = gameObject.activeSelf;
                    m_IsInitialized = true;
                }
                return m_IsActive;
            }
            set
            {
                m_IsActive = value;
                gameObject.SetActive(value);
            }
        }

        private bool m_IsSelected;
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                if (m_IsSelected != value)
                {
                    m_IsSelected = value;
                    OnSelectSite.Invoke(value);
                }
            }
        }
        public GenericEvent<bool> OnSelectSite = new GenericEvent<bool>();

        /// <summary>
        /// Information about this site
        /// </summary>
        public SiteInformation Information { get; set; }

        /// <summary>
        /// State of this site
        /// </summary>
        public SiteState State { get; set; }

        /// <summary>
        /// Configuration of this site
        /// </summary>
        public Data.Visualization.SiteConfiguration Configuration { get; set; }

        public Data.Experience.Dataset.BlocChannelData Data { get; set; }
        public Data.Experience.Dataset.BlocChannelStatistics Statistics { get; set; }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Information = new SiteInformation();
            State = new SiteState();
            Configuration = new Data.Visualization.SiteConfiguration();
        }
        #endregion

        #region Public Methods
        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration(false);
            State.IsBlackListed = Configuration.IsBlacklisted;
            State.IsHighlighted = Configuration.IsHighlighted;
            State.Color = Configuration.Color;
            State.Labels = Configuration.Labels.ToList();
            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        public void SaveConfiguration()
        {
            Configuration.IsBlacklisted = State.IsBlackListed;
            Configuration.IsHighlighted = State.IsHighlighted;
            Configuration.Color = State.Color;
            Configuration.Labels = State.Labels.ToArray();
        }
        public void ResetConfiguration(bool firstCall = true)
        {
            State.IsBlackListed = false;
            State.IsHighlighted = false;
            State.Color = SiteState.DefaultColor;
            State.Labels = new List<string>();
            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}