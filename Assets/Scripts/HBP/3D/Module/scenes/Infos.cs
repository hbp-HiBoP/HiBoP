
/**
 * \file    SceneData.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define MNIPaths, DataS3DScene, MaterialsS3DScene and DisplayedObjects3DView classes
 */

// system
using System.Collections.Generic;
using System.Threading;

// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// States and others basic objects needed in the scene
    /// </summary>
    [System.Serializable]    
    public class SceneStatesInfo
    {
        public enum MeshPart { Left, Right, Both, None };

        #region Properties
        // state
        private bool m_IsIEEGOutdated = true;
        public bool IsIEEGOutdated /**< it true, the amplitudes may need to be updated (ROI, mask, geometry modifications) */
        {
            get
            {
                return m_IsIEEGOutdated;
            }
            set
            {
                m_IsIEEGOutdated = value;
            }
        }
        public bool DisplayCCEPMode = false; // TEST
        public bool IsROICreationModeEnabled = false; /**< is the ROI creation mode enabled ? */
        public bool TimelinesLoaded = false; /**< timelines have been loaded */
        public bool SitesLoaded = false; /**< electrodes have been loaded */
        public bool MeshesLoaded = false;    /**< meshes have been loaded */
        public bool MRILoaded = false;    /**< volume has been loaded */
        public bool CollidersUpdated = false;/**< colluders are up to date */
        public bool IsSceneInitialized { get; set; }
        // parameters
        public bool MarsAtlasModeEnabled = false;
        public bool CutHolesEnabled = false; /**< cuts holes are enabled */
        public int LastPlaneModifiedID = 0;

        // surface
        public MeshPart MeshPartToDisplay = MeshPart.Both; /**< mesh part to be displayed in the scene */
        public DLL.Surface MeshToDisplay = null; /**< reference of the mesh to be displayed */
        public Vector3 MeshCenter = new Vector3(0, 0, 0); /**< center of the loaded mesh */

        // loop check        
        public bool MeshGeometryNeedsUpdate = true; /**< cut planes meshes must be updated */

        public bool IsGeometryUpToDate = false;
        private bool m_IsGeneratorUpToDate = false;
        public bool IsGeneratorUpToDate
        {
            get
            {
                return m_IsGeneratorUpToDate;
            }
            set
            {
                if (value != m_IsGeneratorUpToDate)
                {
                    m_IsGeneratorUpToDate = value;
                    OnUpdateGeneratorState.Invoke(value);
                }
            }
        }
 
        // others                
        public string MeshesLayerName; /**< layer name of all the meshes of the scene */
        public string HiddenMeshesLayerName;
        public bool HideBlacklistedSites = false;

        // work     
        public Vector3 VolumeCenter = new Vector3(0, 0, 0); /**< center of the loaded volume */

        public GenericEvent<bool> OnUpdateGeneratorState = new GenericEvent<bool>();
        #endregion

        #region Public Methods
        public void Reset()
        {
            // state
            DisplayCCEPMode = false; 
            IsROICreationModeEnabled = false;
            TimelinesLoaded = false; 
            SitesLoaded = false;
            MeshesLoaded = false;
            MRILoaded = false; 
            CollidersUpdated = false;

            // loop check  
            MeshGeometryNeedsUpdate = true;
            IsGeneratorUpToDate = false;      
        }
        #endregion
    }

    /// <summary>
    /// All columns common GO objects displayed in the 3D scene
    /// </summary>
    public class DisplayedObjects3DView
    {
        // Parents objects
        public GameObject MeshesParent = null;
        public GameObject BrainSurfaceMeshesParent = null;  /**< brain surface meshes parent of the scene */
        public GameObject InvisibleBrainMeshesParent = null;
        public GameObject BrainCutMeshesParent = null;      /**< brain cut meshes parent of the scene */
        public GameObject SitesMeshesParent = null;         /**< sites meshes parent of the scene */

        // lights
        public GameObject SharedDirectionalLight = null;            /**< shared light between all the scene cameras */

        // Lists of GameObjects to be displayed 
        public List<GameObject> BrainSurfaceMeshes = new List<GameObject>();  /**< brain surface meshes of the scene */
        public List<GameObject> BrainCutMeshes = null;      /**< brain cut meshes of the scene */

        public List<GameObject> InvisibleBrainSurfaceMeshes = null;
    }
}