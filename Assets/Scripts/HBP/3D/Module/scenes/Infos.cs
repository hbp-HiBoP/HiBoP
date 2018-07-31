
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
        #region Properties
        /// <summary>
        /// Is the scene in CCEP mode ?
        /// </summary>
        public bool DisplayCCEPMode = false;
        /// <summary>
        /// Allow the user to add and edit ROI on the scene
        /// </summary>
        public bool IsROICreationModeEnabled = false;
        /// <summary>
        /// Does the colliders need an update ?
        /// </summary>
        public bool CollidersNeedUpdate = true;
        /// <summary>
        /// Is the scene completely initialized ?
        /// </summary>
        public bool IsSceneInitialized = false;
        /// <summary>
        /// Is the scene displayed ?
        /// </summary>
        public bool IsSceneDisplayed = false;
        /// <summary>
        /// Are the sites up to date ?
        /// </summary>
        public bool AreSitesUpdated = false;
        /// <summary>
        /// Is the Mars Atlas mode enabled ?
        /// </summary>
        public bool MarsAtlasModeEnabled = false;
        /// <summary>
        /// Are we cutting the holes ?
        /// </summary>
        public bool CutHolesEnabled = false;
        /// <summary>
        /// Last modified plane (used to display the cut circles)
        /// </summary>
        public int LastPlaneModifiedID = 0;
        /// <summary>
        /// Does this scene use simplified meshes ?
        /// </summary>
        public bool UseSimplifiedMeshes { get; set; }
        /// <summary>
        /// Mesh part to be displayed in the scene
        /// </summary>
        public Data.Enums.MeshPart MeshPartToDisplay = Data.Enums.MeshPart.Both;
        /// <summary>
        /// Mesh being displayed in the scene
        /// </summary>
        public DLL.Surface MeshToDisplay = null;
        /// <summary>
        /// Simplified mesh to be used in the scene
        /// </summary>
        public DLL.Surface SimplifiedMeshToUse = null;
        /// <summary>
        /// Center of the loaded mesh
        /// </summary>
        public Vector3 MeshCenter = new Vector3(0, 0, 0);
        /// <summary>
        /// Center of the loaded mri
        /// </summary>
        public Vector3 VolumeCenter = new Vector3(0, 0, 0);
        /// <summary>
        /// Does the mesh geometry need an update (cut, changing the mesh etc.)
        /// </summary>
        public bool MeshGeometryNeedsUpdate = true;
        private bool m_IsGeneratorUpToDate = false;
        /// <summary>
        /// Is the iEEG generator up to date ?
        /// </summary>
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
        /// <summary>
        /// Name of the layer containing the meshes
        /// </summary>
        public string MeshesLayerName;
        /// <summary>
        /// Name of the layer containing the hidden meshes (e.g. the surface mesh when cutting)
        /// </summary>
        public string HiddenMeshesLayerName;
        /// <summary>
        /// Hide blacklisted sites
        /// </summary>
        public bool HideBlacklistedSites = false;
        /// <summary>
        /// Show all sites even if they are not in a ROI
        /// </summary>
        public bool ShowAllSites = false;
        #endregion

        #region Events
        public GenericEvent<bool> OnUpdateGeneratorState = new GenericEvent<bool>();
        #endregion
    }
}