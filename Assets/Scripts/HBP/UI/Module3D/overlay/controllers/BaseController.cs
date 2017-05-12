
/**
 * \file    BaseController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UIOverlay, BaseOverlayController,IndividualSceneOverlayController,BothScenesOverlayController classes
 */

// unity
using UnityEngine;

// hbp
using HBP.Module3D.Cam;


namespace HBP.Module3D
{
    /// <summary>
    /// UI overlay activity manager
    /// </summary>
    public class UIOverlay
    {
        public void setActivity(bool activity)
        {
            mainUITransform.gameObject.SetActive(activity);
        }

        public Transform mainUITransform;
    }

    /// <summary>
    /// Base for all overlays controllers
    /// </summary>
    abstract public class BaseOverlayController : MonoBehaviour
    {
        #region Properties
        protected CamerasManager m_CamerasManager;
        #endregion

        #region Public Methods
        public abstract void UpdatePosition();
        public abstract void UpdateUI();
        #endregion

        public virtual void Initialize(CamerasManager camerasManager)
        {
            m_CamerasManager = camerasManager;
        }
    }


    /// <summary>
    /// Base for individual scene overlay controllers
    /// </summary>
    abstract public class IndividualSceneOverlayController : BaseOverlayController
    {
        #region Properties
        [SerializeField,HideInInspector]
        protected Mode m_CurrentMode = null;
        public Mode CurrentMode
        {
            get { return m_CurrentMode; }
            set
            {
                m_CurrentMode = value;
                UpdateUI();
            }
        }
        protected Base3DScene m_Scene = null;

        [SerializeField, Candlelight.PropertyBackingField]
        protected bool m_IsVisibleFromScene = true;
        public bool IsVisibleFromScene
        {
            get { return m_IsVisibleFromScene; }
            set
            {
                m_IsVisibleFromScene = value;
                UpdateUI();
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected bool m_IsEnoughtRoom = true;
        public bool IsEnoughtRoom
        {
            get { return m_IsEnoughtRoom; }
            set { m_IsEnoughtRoom = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected bool m_CurrentActivity = false;
        public bool CurrentActivity
        {
            get { return m_CurrentActivity; }
            set
            {
                m_CurrentActivity = value;
                UpdateUI();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="camerasManager"></param>
        public void Initialize(Base3DScene scene, CamerasManager camerasManager)
        {
            base.Initialize(camerasManager);
            m_Scene = scene;
        }
        #endregion
    }

    /// <summary>
    /// Base for both scenes overlay controllers
    /// </summary>
    abstract public class BothScenesOverlayController : BaseOverlayController
    {
        [SerializeField, Candlelight.PropertyBackingField]
        protected bool m_IsVisibleFromSinglePatientScene = true;
        public bool IsVisibleFromSinglePatientScene
        {
            get { return m_IsVisibleFromSinglePatientScene; }
            set
            {
                m_IsVisibleFromSinglePatientScene = value;
                UpdateUI();
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected bool m_IsVisibleFromMultiPatientsScene = true;
        public bool IsVisibleFromMultiPatientsScene
        {
            get { return m_IsVisibleFromMultiPatientsScene; }
            set
            {
                m_IsVisibleFromMultiPatientsScene = value;
                UpdateUI();
            }
        }

        protected bool m_IsEnoughtRoomInSinglePatientScene = true;
        protected bool m_IsEnoughtRoomInMultiPatientsScene = true;

        protected bool m_CurrentSinglePatientActivity = false;
        protected bool m_CurrentMultiPatientsActivity = false;
        protected Mode m_CurrentSinglePatientMode = null;
        protected Mode m_CurrentMultiPatientsMode = null;

        protected SinglePatient3DScene m_SinglePatientScene = null;
        protected MultiPatients3DScene m_MultiPatientsScene = null;        

        public void Initialize(ScenesManager scenesManager)
        {
            //base.Initialize(scenesManager.CamerasManager);
            //m_SinglePatientScene = scenesManager.SinglePatientScene;
            //m_MultiPatientsScene = scenesManager.MultiPatientsScene;
        }

        /// <summary>
        /// Update UI with activity and the current mode
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="mode"></param>
        public void SetActivity(bool activity, Mode mode)
        {
            switch (mode.Type)
            {
                case SceneType.SinglePatient:
                    m_CurrentSinglePatientActivity = activity;
                    m_CurrentSinglePatientMode = mode;
                    break;
                case SceneType.MultiPatients:
                    m_CurrentMultiPatientsActivity = activity;
                    m_CurrentMultiPatientsMode = mode;
                    break;
                default:
                    break;
            }
            UpdateUI();
        } 
    }
}