using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Interfaces;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class containing information about a site
    /// </summary>
    /// <remarks>
    /// The information available in this class does not depend on which column the site is
    /// One instance of this class is required per site in the scene
    /// </remarks>
    public class SiteInformation
    {
        #region Properties
        /// <summary>
        /// Reference to the site data of this site
        /// </summary>
        public Data.Site SiteData { get; set; }
        /// <summary>
        /// Reference to the patient this site belongs to
        /// </summary>
        public Data.Patient Patient { get; set; }
        /// <summary>
        /// Raw name of the site (as read in the implantation file)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Global Index of the site (same index as in the raw site list)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// ID of the patient this site belongs to
        /// </summary>
        public string PatientID
        {
            get
            {
                return Patient.ID;
            }
        }
        /// <summary>
        /// Full ID of this site
        /// </summary>
        public string FullID
        {
            get
            {
                return PatientID + "_" + Name;
            }
        }
        /// <summary>
        /// Format the name of the string with information from the patient it is from in order to display it properly
        /// </summary>
        public string DisplayedName
        {
            get
            {
                return Name + " (" + Patient.Name + " - " + Patient.Place + " - " + Patient.Date + ")";
            }
        }

        /// <summary>
        /// Initial position of this site
        /// </summary>
        public Vector3 DefaultPosition { get; set; }
        #endregion
    }

    /// <summary>
    /// This class describes the current state of the site
    /// </summary>
    /// <remarks>
    /// The information avaiable in this class depends on which column the site is
    /// Each instance of the <see cref="Site"/> class has its own instance of this class
    /// </remarks>
    public class SiteState
    {
        #region Properties
        /// <summary>
        /// Default color of any site
        /// </summary>
        public static Color DefaultColor = new Color(0.53f, 0.15f, 0.15f);

        /// <summary>
        /// Is the site completely masked ?
        /// </summary>
        /// <remarks>
        /// Usually, that means this site contains no activity value
        /// </remarks>
        public bool IsMasked { get; set; }

        /// <summary>
        /// Is the site out of the selected ROI ?
        /// </summary>
        public bool IsOutOfROI { get; set; }

        private bool m_IsFiltered = true;
        /// <summary>
        /// Is the site filtered (using the site list on the right)
        /// </summary>
        public bool IsFiltered
        {
            get
            {
                return m_IsFiltered;
            }
            set
            {
                m_IsFiltered = value;
                OnChangeState.Invoke();
            }
        }

        private bool m_IsBlackListed;
        /// <summary>
        /// Is the site blacklisted ?
        /// </summary>
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
        /// <summary>
        /// Is the site highlighted ?
        /// </summary>
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
        /// <summary>
        /// Color of the site (the color will be applied if the site is in a regular state)
        /// </summary>
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

        /// <summary>
        /// Labels of the site
        /// </summary>
        public List<string> Labels { get; set; } = new List<string>();
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing any parameter of this class
        /// </summary>
        public UnityEvent OnChangeState = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a new label (if the label is not already in the list)
        /// </summary>
        /// <param name="label">Label to be added</param>
        public void AddLabel(string label)
        {
            if (!Labels.Contains(label))
            {
                Labels.Add(label);
                OnChangeState.Invoke();
            }
        }
        /// <summary>
        /// Remove a label from the list
        /// </summary>
        /// <param name="label">Label to be removed</param>
        public void RemoveLabel(string label)
        {
            Labels.Remove(label);
            OnChangeState.Invoke();
        }
        /// <summary>
        /// Remove all labels
        /// </summary>
        public void RemoveAllLabels()
        {
            Labels.Clear();
            OnChangeState.Invoke();
        }
        /// <summary>
        /// Copy a state to this state
        /// </summary>
        /// <param name="state">State to be copied</param>
        public void ApplyState(SiteState state)
        {
            ApplyState(state.IsBlackListed, state.IsHighlighted, state.Color, state.Labels);
        }
        /// <summary>
        /// Set all values of the state at once
        /// </summary>
        /// <param name="blacklisted">Is the site blacklisted ?</param>
        /// <param name="highlighted">Is the site highlighted ?</param>
        /// <param name="color">Color of the site</param>
        /// <param name="labels">Labels of the site</param>
        public void ApplyState(bool blacklisted, bool highlighted, Color color, IEnumerable<string> labels)
        {
            m_IsBlackListed = blacklisted;
            m_IsHighlighted = highlighted;
            m_Color = color;
            Labels = labels.ToList();
            OnChangeState.Invoke();
        }
        #endregion
    }

    /// <summary>
    /// Class defining a site in the scene
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
        /// <summary>
        /// Is the site selected ?
        /// </summary>
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

        /// <summary>
        /// Information about this site (common to all sites with the same ID in the scene)
        /// </summary>
        public SiteInformation Information { get; set; }

        /// <summary>
        /// State of this site (one per site)
        /// </summary>
        public SiteState State { get; set; }

        /// <summary>
        /// Configuration of this site
        /// </summary>
        public Data.SiteConfiguration Configuration { get; set; }

        /// <summary>
        /// Associated data
        /// </summary>
        public Data.BlocChannelData Data { get; set; }
        /// <summary>
        /// Associated data statistics
        /// </summary>
        public Data.BlocChannelStatistics Statistics { get; set; }
        #endregion

        #region Events
        /// <summary>
        /// Event called when selecting or unselecting this site
        /// </summary>
        public GenericEvent<bool> OnSelectSite = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            Information = new SiteInformation();
            State = new SiteState();
            Configuration = new Data.SiteConfiguration();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load a configuration to this site from the configuration in the associated visualization
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            State.IsBlackListed = Configuration.IsBlacklisted;
            State.IsHighlighted = Configuration.IsHighlighted;
            State.Color = Configuration.Color;
            State.Labels = Configuration.Labels.ToList();

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Save the configuration of this site to the configuration in the associated visualization
        /// </summary>
        public void SaveConfiguration()
        {
            Configuration.IsBlacklisted = State.IsBlackListed;
            Configuration.IsHighlighted = State.IsHighlighted;
            Configuration.Color = State.Color;
            Configuration.Labels = State.Labels.ToArray();
        }
        /// <summary>
        /// Reset the configuration of this site
        /// </summary>
        public void ResetConfiguration()
        {
            State.IsBlackListed = false;
            State.IsHighlighted = false;
            State.Color = SiteState.DefaultColor;
            State.Labels = new List<string>();

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}