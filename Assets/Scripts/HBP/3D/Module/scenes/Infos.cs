﻿
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

namespace HBP.Module3D
{
    /// <summary>
    /// States and others basic objects needed in the scene
    /// </summary>
    [System.Serializable]    
    public class SceneStatesInfo
    {
        public enum MeshPart { Left, Right, Both, None };
        public enum MeshType { Hemi, White, Inflated };

        #region Properties
        // mutex
        public ReaderWriterLock RWLock = new ReaderWriterLock();
        public float CurrentComputingState = 0f;

        // state
        public bool IsIEEGOutdated = true; /**< it true, the amplitudes may need to be updated (ROI, mask, geometry modifications) */
        public bool IsComparingSites = false; /**< if true, the next plot clicked will be used to be compared with the current one */
        public bool DisplayCCEPMode = false; // TEST
        public bool IsROICreationModeEnabled = false; /**< is the ROI creation mode enabled ? */
        public bool TimelinesLoaded = false; /**< timelines have been loaded */
        public bool SitesLoaded = false; /**< electrodes have been loaded */
        public bool MeshesLoaded = false;    /**< meshes have been loaded */
        public bool MRILoaded = false;    /**< volume has been loaded */
        public bool CollidersUpdated = false;/**< colluders are up to date */
        public bool HemiMeshesAvailables = false;    /**< hemi meshes are availables */
        public bool WhiteMeshesAvailables = false;    /**< white meshes are availables */
        public bool WhiteInflatedMeshesAvailables = false; /**< white inflated meshes are availables */
        // parameters
        public bool MarsAtlasModeEnabled = false;
        public bool MarsAtlasParcelsLoaed = false;
        public bool CutHolesEnabled = false; /**< cuts holes are enabled */
        public int LastPlaneModifiedID = 0;

        // surface
        public MeshPart MeshPartToDisplay = MeshPart.Both; /**< mesh part to be displayed in the scene */
        public MeshType MeshTypeToDisplay = MeshType.Hemi; /**< mesh type to be displayed in the scene */
        public DLL.Surface MeshToDisplay = null; /**< reference of the mesh to be displayed */
        public Vector3 MeshCenter = new Vector3(0, 0, 0); /**< center of the loaded mesh */

        // loop check        
        public bool CutMeshGeometryNeedsUpdate = true; /**< cut planes meshes must be updated */

        public bool IsGeometryUpToDate = false;
        public bool IsGeneratorUpToDate = false;  /**< texture generator is up to date */
 
        // others                
        public string MeshesLayerName; /**< layer name of all the meshes of the scene */

        // work     
        public Vector3 VolumeCenter = new Vector3(0, 0, 0); /**< center of the loaded volume */
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
            HemiMeshesAvailables = false;
            WhiteMeshesAvailables = false; 
            WhiteInflatedMeshesAvailables = false;            

            // loop check  
            CutMeshGeometryNeedsUpdate = true;
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
        public GameObject BrainSurfaceMeshesParent = null;  /**< brain surface meshes parent of the scene */
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