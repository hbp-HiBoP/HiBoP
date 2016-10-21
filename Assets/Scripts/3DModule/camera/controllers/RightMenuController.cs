
/**
 * \file    RightMenuController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define RightMenuController class
 */

// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for managing the menu UI at the right of the scene
    /// </summary>
    public class RightMenuController : MonoBehaviour, UICameraOverlay
    {
        #region members

        /// <summary>
        /// ID of the SP scene buttons
        /// </summary>
        public enum SpButtonsId : int
        {
            add_view, remove_view, tri_erasing, escape_tri_erasing, add_IRMF, remove_IRMF, amp_mode, latency_mode, brain_left, brain_right, brain_both
        };

        /// <summary>
        /// ID of the MP scene buttons
        /// </summary>
        public enum MpButtonsId : int
        {
            add_view, remove_view, tri_erasing, escape_tri_erasing, ROI, escape_ROI, brain_inflated, brain_white, brain_hemi, add_IRMF, remove_IRMF, brain_left, brain_right, brain_both
        };

        public Transform m_buttonsRightPanel = null; /**< right panel transform */

        private Transform m_spButtons = null; /**< SP buttons parent transform */
        public Transform SPButtons { get { return m_spButtons; } }

        private Transform m_mpButtons = null; /**< MP buttons parent transform */
        public Transform MPButtons { get { return m_mpButtons; } }


        private List<Button> m_spButtonsList = new List<Button>(); /**< SP list of buttons */
        private List<Button> m_mpButtonsList = new List<Button>(); /**< MP list of buttons */

        private List<GameObject> m_spInter = new List<GameObject>(); /**< SP inter lines */
        private List<GameObject> m_mpInter = new List<GameObject>(); /**< MP inter lines */

        private List<GameObject> m_spBlankButtonsList = new List<GameObject>(); /**< SP list of buttons */
        private List<GameObject> m_mpBlankButtonsList = new List<GameObject>(); /**< MP list of buttons */

        private int currentSpNbButtons = 0;
        private int currentMpNbButtons = 0;


        private SP3DScene m_spScene = null;
        private MP3DScene m_mpScene = null;
        private Cam.CamerasManager m_camerasManager = null;

        // events
        public NoParamEvent m_roiMenuStateEvent = new NoParamEvent();


        #endregion members

        #region mono_behaviour

        //// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            m_spButtons = m_buttonsRightPanel.Find("buttons sp");
            m_mpButtons = m_buttonsRightPanel.Find("buttons mp");

            // sp buttons order
            m_spButtonsList.Add(m_spButtons.Find("add view button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("remove view button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("tri erasing button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("escape tri erasing button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("add IRMF button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("remove IRMF button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("amp mode button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("latency mode button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("brain left button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("brain right button").GetComponent<Button>());
            m_spButtonsList.Add(m_spButtons.Find("brain both button").GetComponent<Button>());

            // sp inter
            m_spInter.Add(m_spButtons.Find("inter IRMF").gameObject);
            m_spInter.Add(m_spButtons.Find("inter DATA").gameObject);
            m_spInter.Add(m_mpButtons.Find("inter mesh type").gameObject);
            m_spInter.Add(m_spButtons.Find("inter view").gameObject);

            // mp buttons order
            m_mpButtonsList.Add(m_mpButtons.Find("add view button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("remove view button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("tri erasing button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("escape tri erasing button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("ROI button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("escape ROI button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("brain inflated button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("brain white button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("brain hemi button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("add IRMF button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("remove IRMF button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("brain left button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("brain right button").GetComponent<Button>());
            m_mpButtonsList.Add(m_mpButtons.Find("brain both button").GetComponent<Button>());

            // sp inter
            m_mpInter.Add(m_mpButtons.Find("inter ROI").gameObject);
            m_mpInter.Add(m_mpButtons.Find("inter IRMF").gameObject);
            m_mpInter.Add(m_mpButtons.Find("inter mesh type").gameObject);
            m_mpInter.Add(m_mpButtons.Find("inter view").gameObject);

            // sp blanks
            m_spBlankButtonsList.Add(m_spButtons.Find("blank1").gameObject);
            m_spBlankButtonsList.Add(m_spButtons.Find("blank2").gameObject);

            // mp blanks
            m_mpBlankButtonsList.Add(m_mpButtons.Find("blank1").gameObject);
            m_mpBlankButtonsList.Add(m_mpButtons.Find("blank2").gameObject);
        }


        #endregion mono_behaviour

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            m_spScene = scenesManager.SPScene;
            m_mpScene = scenesManager.MPScene;
            m_camerasManager = scenesManager.CamerasManager;

            // add view listener
            // sp
            m_spButtonsList[(int)SpButtonsId.add_view].onClick.AddListener(delegate
            {
                Listeners.addView(m_camerasManager, true);
                m_spScene.setCurrentModeSpecifications(true);
            });
            // mp
            m_mpButtonsList[(int)MpButtonsId.add_view].onClick.AddListener(delegate
            {
                Listeners.addView(m_camerasManager, false);
                m_mpScene.setCurrentModeSpecifications(true);
            });

            // remove view listener
            // sp
            m_spButtonsList[(int)SpButtonsId.remove_view].onClick.AddListener(delegate
            {
                Listeners.removeView(m_camerasManager, true);
                m_spScene.setCurrentModeSpecifications(true);
            });
            // mp
            m_mpButtonsList[(int)MpButtonsId.remove_view].onClick.AddListener(delegate
            {
                Listeners.removeView(m_camerasManager, false);
                m_mpScene.setCurrentModeSpecifications(true);
            });

            // tri erasing
            // sp
            m_spButtonsList[(int)SpButtonsId.tri_erasing].onClick.AddListener(delegate
            {
                m_spScene.enableTriErasingMode();
            });

            // escape tri erasing
            // sp
            m_spButtonsList[(int)SpButtonsId.escape_tri_erasing].onClick.AddListener(delegate
            {
                m_spScene.disableTriErasingMode();
            });

            // ROI creation
            m_mpButtonsList[(int)MpButtonsId.ROI].onClick.AddListener(delegate {
                m_mpScene.enableROICreationMode();
                m_roiMenuStateEvent.Invoke();
            });

            // escape ROI creation
            m_mpButtonsList[(int)MpButtonsId.escape_ROI].onClick.AddListener(delegate {
                m_mpScene.disableROICreationMode();
                m_roiMenuStateEvent.Invoke();
            });

            // hemi listener
            // mp
            m_mpButtonsList[(int)MpButtonsId.brain_hemi].onClick.AddListener(delegate { m_mpScene.setDisplayedMeshType(SceneStatesInfo.MeshType.Hemi); });
            // white listener
            // mp
            m_mpButtonsList[(int)MpButtonsId.brain_white].onClick.AddListener(delegate { m_mpScene.setDisplayedMeshType(SceneStatesInfo.MeshType.White); });
            // inflated listener
            // mp
            m_mpButtonsList[(int)MpButtonsId.brain_inflated].onClick.AddListener(delegate { m_mpScene.setDisplayedMeshType(SceneStatesInfo.MeshType.Inflated); });

            // left listener
            // sp
            m_spButtonsList[(int)SpButtonsId.brain_left].onClick.AddListener(delegate { m_spScene.setDisplayedMeshPart(SceneStatesInfo.MeshPart.Left); });
            // mp
            m_mpButtonsList[(int)MpButtonsId.brain_left].onClick.AddListener(delegate { m_mpScene.setDisplayedMeshPart(SceneStatesInfo.MeshPart.Left); });
            // right listener
            // sp
            m_spButtonsList[(int)SpButtonsId.brain_right].onClick.AddListener(delegate { m_spScene.setDisplayedMeshPart(SceneStatesInfo.MeshPart.Right); });
            // mp
            m_mpButtonsList[(int)MpButtonsId.brain_right].onClick.AddListener(delegate { m_mpScene.setDisplayedMeshPart(SceneStatesInfo.MeshPart.Right); });
            // both listener
            // sp
            m_spButtonsList[(int)SpButtonsId.brain_both].onClick.AddListener(delegate { m_spScene.setDisplayedMeshPart(SceneStatesInfo.MeshPart.Both); });
            // mp
            m_mpButtonsList[(int)MpButtonsId.brain_both].onClick.AddListener(delegate { m_mpScene.setDisplayedMeshPart(SceneStatesInfo.MeshPart.Both); });

            // add/remove IRMF
            // sp
            m_spButtonsList[(int)SpButtonsId.add_IRMF].onClick.AddListener(delegate { StaticVisuComponents.HBPMain.addIRMF(true); });
            m_spButtonsList[(int)SpButtonsId.remove_IRMF].onClick.AddListener(delegate { StaticVisuComponents.HBPMain.removeLastIRMF(true); });
            // mp            
            m_mpButtonsList[(int)MpButtonsId.add_IRMF].onClick.AddListener(delegate { StaticVisuComponents.HBPMain.addIRMF(false); });
            m_mpButtonsList[(int)MpButtonsId.remove_IRMF].onClick.AddListener(delegate { StaticVisuComponents.HBPMain.removeLastIRMF(false); });

            // amp mode
            // sp
            m_spButtonsList[(int)SpButtonsId.amp_mode].onClick.AddListener(delegate { m_spScene.setLatencyDisplayMode(false); });
            // latency mode
            // sp
            m_spButtonsList[(int)SpButtonsId.latency_mode].onClick.AddListener(delegate { m_spScene.setLatencyDisplayMode(true); });
        }

        /// <summary>
        /// Update the state of a button
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idButton"></param>
        /// <param name="active"></param>
        /// <param name="interactable"></param>
        private void setStateButton(bool spScene, int idButton, bool active, bool interactable)
        {
            if (spScene)
            {
                m_spButtonsList[idButton].interactable = interactable;
                m_spButtonsList[idButton].gameObject.SetActive(active);
            }
            else
            {
                m_mpButtonsList[idButton].interactable = interactable;
                m_mpButtonsList[idButton].gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Update the UI components states with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void setUIActivity(Mode mode)
        {
            if (mode.m_sceneSp)
            {
                // IRFM
                bool addIRMFDispo;
                bool removeIRMFDispo;
                if (m_spScene.getIRMFColumnsNb() == 0)
                {
                    addIRMFDispo = true;
                    removeIRMFDispo = false;
                }
                else if (m_spScene.getIRMFColumnsNb() == 2)
                {
                    addIRMFDispo = false;
                    removeIRMFDispo = true;
                }
                else
                {
                    addIRMFDispo = true;
                    removeIRMFDispo = true;
                }

                // latency
                bool latencyMode = m_spScene.isLatencyMode();
                if(latencyMode)
                {
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.amp_mode, true, true);  
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.latency_mode, false, false);  
                }
                else
                {
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.amp_mode, false, false);  
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.latency_mode, true, true);  
                }

                bool brainLeftInteract = m_spScene.data_.meshPartToDisplay != SceneStatesInfo.MeshPart.Left;
                bool brainRightInteract = m_spScene.data_.meshPartToDisplay != SceneStatesInfo.MeshPart.Right;
                bool brainBothInteract = m_spScene.data_.meshPartToDisplay != SceneStatesInfo.MeshPart.Both;

                switch (mode.m_idMode)
                {                    
                    case Mode.ModesId.NoPathDefined:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.amp_mode, false, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.latency_mode, false, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, false, brainLeftInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, false, brainRightInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, false, brainBothInteract);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(false);
                        m_spInter[2].SetActive(false);
                        m_spInter[3].SetActive(false);
                        currentSpNbButtons = 2;
                        break;
                    case Mode.ModesId.MinPathDefined:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, true, brainLeftInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, true, brainRightInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, true, brainBothInteract);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(false);
                        m_spInter[2].SetActive(true);
                        m_spInter[3].SetActive(true);
                        currentSpNbButtons = 8;
                        break;
                    case Mode.ModesId.AllPathDefined:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, true, brainLeftInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, true, brainRightInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, true, brainBothInteract);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(true);
                        m_spInter[2].SetActive(true);
                        m_spInter[3].SetActive(true);
                        currentSpNbButtons = 8;
                        break;
                    case Mode.ModesId.ComputingAmplitudes:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, true, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, true, false);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, true, false);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(true);
                        m_spInter[2].SetActive(true);
                        m_spInter[3].SetActive(true);
                        currentSpNbButtons = 8;
                        break;
                    case Mode.ModesId.AmplitudesComputed:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);   
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, false); 
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, true, brainLeftInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, true, brainRightInteract);  
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, true, brainBothInteract);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(true);
                        m_spInter[2].SetActive(true);
                        m_spInter[3].SetActive(true);
                        currentSpNbButtons = 8;
                        break;
                    case Mode.ModesId.TriErasing:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, true);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, false, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, false, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, false, brainBothInteract);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(false);
                        m_spInter[2].SetActive(false);
                        m_spInter[3].SetActive(false);
                        currentSpNbButtons = 5;
                        break;
                    case Mode.ModesId.AmpNeedUpdate:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, true, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, true, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, true, brainBothInteract);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(true);
                        m_spInter[2].SetActive(true);
                        m_spInter[3].SetActive(true);
                        currentSpNbButtons = 8;
                        break;   
                    case Mode.ModesId.Error:
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_left, false, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_right, false, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)SpButtonsId.brain_both, false, brainBothInteract);
                        m_spInter[0].SetActive(false);
                        m_spInter[1].SetActive(false);
                        m_spInter[2].SetActive(false);
                        m_spInter[3].SetActive(false);
                        currentSpNbButtons = 2;
                        break;
                    default:
                        Debug.LogError("Error : setUIActivity");
                        break;
                }

                // views
                if (m_camerasManager.getNumberOfViews(true) == 1)
                {
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_view, true, false);
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_view, true, true);
                }
                else if (m_camerasManager.getNumberOfViews(true) == m_camerasManager.m_maxViewsLine)
                {
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_view, true, true);
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_view, true, false);
                }
                else
                {
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.remove_view, true, true);
                    setStateButton(mode.m_sceneSp, (int)SpButtonsId.add_view, true, true);
                }

            }
            else
            {
                // IRFM
                bool addIRMFDispo;
                bool removeIRMFDispo;
                if (m_mpScene.getIRMFColumnsNb() == 0)
                {
                    addIRMFDispo = true;
                    removeIRMFDispo = false;
                }
                else if (m_mpScene.getIRMFColumnsNb() == 2)
                {
                    addIRMFDispo = false;
                    removeIRMFDispo = true;
                }
                else
                {
                    addIRMFDispo = true;
                    removeIRMFDispo = true;
                }

                bool brainLeftInteract = m_mpScene.data_.meshPartToDisplay != SceneStatesInfo.MeshPart.Left;
                bool brainRightInteract = m_mpScene.data_.meshPartToDisplay != SceneStatesInfo.MeshPart.Right;
                bool brainBothInteract = m_mpScene.data_.meshPartToDisplay != SceneStatesInfo.MeshPart.Both;

                bool brainHemiInteract = m_mpScene.data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Hemi;
                bool brainWhiteInteract = m_mpScene.data_.meshTypeToDisplay != SceneStatesInfo.MeshType.White;
                bool brainInflatedInteract = m_mpScene.data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated;

                switch (mode.m_idMode)
                {
                    case Mode.ModesId.NoPathDefined:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, false, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, false, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, false, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, false, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, false, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, false, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(false);
                        m_mpInter[2].SetActive(false);
                        m_mpInter[3].SetActive(false);
                        currentMpNbButtons = 2;
                        break;
                    case Mode.ModesId.MinPathDefined:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, true, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, true, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, true, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, true, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, true, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, true, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(false);
                        m_mpInter[2].SetActive(true);
                        m_mpInter[3].SetActive(true);
                        currentMpNbButtons = 11;
                        break;
                    case Mode.ModesId.AllPathDefined:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, true, true);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, true, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, true, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, true, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, true, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, true, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, true, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(true);
                        m_mpInter[2].SetActive(true);
                        m_mpInter[3].SetActive(true);
                        currentMpNbButtons = 11;
                        break;
                    case Mode.ModesId.ComputingAmplitudes:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, true, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, true, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, true, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, true, false);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(true);
                        m_mpInter[2].SetActive(true);
                        m_mpInter[3].SetActive(true);
                        currentMpNbButtons = 11;
                        break;
                    case Mode.ModesId.AmplitudesComputed:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, true, true);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, true, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, true, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, true, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, true, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, true, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, true, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(true);
                        m_mpInter[2].SetActive(true);
                        m_mpInter[3].SetActive(true);
                        currentMpNbButtons = 11;
                        break;
                    case Mode.ModesId.TriErasing:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, true);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, false, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, false, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, false, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, false, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, false, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, false, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(true);
                        m_mpInter[2].SetActive(true);
                        m_mpInter[3].SetActive(true);
                        currentMpNbButtons = 7;
                        break;
                    case Mode.ModesId.ROICreation:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, true, true);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, false, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, false, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, false, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, false, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, false, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, false, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(true);
                        m_mpInter[2].SetActive(true);
                        m_mpInter[3].SetActive(true);
                        currentMpNbButtons = 5;
                        break;
                    case Mode.ModesId.AmpNeedUpdate:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, true, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, true, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, true, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, true, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, true, addIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, true, removeIRMFDispo);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, true, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, true, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, true, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(true);
                        m_mpInter[2].SetActive(true);
                        m_mpInter[3].SetActive(true);
                        currentMpNbButtons = 11;
                        break;
                    case Mode.ModesId.Error:
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_tri_erasing, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.escape_ROI, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_inflated, false, brainInflatedInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_white, false, brainWhiteInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_hemi, false, brainHemiInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_IRMF, false, false);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_left, false, brainLeftInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_right, false, brainRightInteract);
                        setStateButton(mode.m_sceneSp, (int)MpButtonsId.brain_both, false, brainBothInteract);
                        m_mpInter[0].SetActive(false);
                        m_mpInter[1].SetActive(false);
                        m_mpInter[2].SetActive(false);
                        m_mpInter[3].SetActive(false);
                        currentMpNbButtons = 2;
                        break;
                    default:
                        Debug.LogError("Error : setUIActivity");
                        break;
                }

                // views
                if (m_camerasManager.getNumberOfViews(false) == 1)
                {
                    setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_view, true, false);
                    setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_view, true, true);
                }
                else if (m_camerasManager.getNumberOfViews(false) == m_camerasManager.m_maxViewsLine)
                {
                    setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_view, true, true);
                    setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_view, true, false);
                }
                else
                {
                    setStateButton(mode.m_sceneSp, (int)MpButtonsId.remove_view, true, true);
                    setStateButton(mode.m_sceneSp, (int)MpButtonsId.add_view, true, true);
                }
            }


            // update blanks buttons activity
            int diff = currentSpNbButtons - currentMpNbButtons;

            
            if (diff < 0)
            {
                diff = -diff;

                for(int ii = 0; ii < m_spBlankButtonsList.Count; ++ii)
                {
                    bool enableBlank = ii < diff;
                    m_spBlankButtonsList[ii].SetActive(enableBlank);
                    m_mpBlankButtonsList[ii].SetActive(false);
                }                
            }
            else if(diff > 0)
            {
                for (int ii = 0; ii < m_spBlankButtonsList.Count; ++ii)
                {
                    bool enableBlank = ii < diff;
                    m_spBlankButtonsList[ii].SetActive(false);
                    m_mpBlankButtonsList[ii].SetActive(enableBlank);
                }
            }
            else
            {
                for (int ii = 0; ii < m_spBlankButtonsList.Count; ++ii)
                {
                    m_spBlankButtonsList[ii].SetActive(false);
                    m_mpBlankButtonsList[ii].SetActive(false);
                }
            }
        }

        #endregion functions

    }

}