
/**
 * \file    SceneData.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define MNIPaths, DataS3DScene, MaterialsS3DScene and DisplayedObjects3DView classes
 */

// system
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// States and others basic objects needed in the scene
    /// </summary>
    [System.Serializable]    
    public class SceneStatesInfo
    {
        public enum MeshPart : int { Left, Right, Both};
        public enum MeshType : int { Hemi, White, Inflated};

        #region members

        // mutex
        public ReaderWriterLock rwl = new ReaderWriterLock();
        public float currentComputingState = 0f;

        // state
        public bool iEEGOutdated = false; /**< it true, the amplitudes may need to be updated (ROI, mask, geometry modifications) */
        public bool comparePlot = false; /**< if true, the next plot clicked will be used to be compared with the current one */
        public bool colorMani = false; // COLORS FOR MANI
        public bool displayLatenciesMode = false; // TEST
        public bool ROICreationMode = false; /**< is the ROI creation mode enabled ? */
        public bool timelinesLoaded = false; /**< timelines have been loaded */
        public bool electodesLoaded = false; /**< electrodes have been loaded */
        public bool meshesLoaded = false;    /**< meshes have been loaded */
        public bool volumeLoaded = false;    /**< volume has been loaded */
        public bool collidersUpdated = false;/**< colluders are up to date */
        public bool hemiMeshesAvailables = false;    /**< hemi meshes are availables */
        public bool whiteMeshesAvailables = false;    /**< white meshes are availables */
        public bool whiteInflatedMeshesAvailables = false; /**< white inflated meshes are availables */
        // parameters
        public bool holesEnabled = false; /**< cuts holes are enabled */
        public int defaultNbOfCutsPerPlane = 500; /**< default number of cuts for the planes */
        public int lastIdPlaneModified = 0;

        // surface
        public MeshPart meshPartToDisplay = MeshPart.Both; /**< mesh part to be displayed in the scene */
        public MeshType meshTypeToDisplay = MeshType.Hemi; /**< mesh type to be displayed in the scene */
        public DLL.Surface meshToDisplay = null; /**< reference of the mesh to be displayed */
        public Vector3 meshCenter = new Vector3(0, 0, 0); /**< center of the loaded mesh */

        // loop check        
        public bool updateGeometry = false;     /**< the geometry of the mesh must be updated */
        public bool updateIRMF = false;         /**< the IRFM cuts must be updated */
        public bool updateImageCuts = false;
        public bool updateCutPlanes = true; /**< cut planes meshes must be updated */
        public bool generatorIsComputing = false; /**< texture generator thread is running */
        public bool generatorUpToDate = false;  /**< texture generator is up to date */

        public List<bool> texturesCutToUpdateMask = new List<bool>(); /**< planes cut which must be updated */

        //public List<bool> nullifiedTexturesCoords = new List<bool>(); /**< mesh textures coords that must be nullified (size == meshsplitNb)*/
 
        // others                
        public string MeshesLayerName; /**< layer name of all the meshes of the scene */

        // work     
        public Vector3 volumeCenter = new Vector3(0, 0, 0); /**< center of the loaded volume */
        public List<int> numberOfCutsPerPlane = new List<int>(); /**< number of cuts per plane list */
        public List<bool> removeFrontPlaneList = null; // TO BE REMOVED

        #endregion members

        public void reset()
        {
            // state
            colorMani = false;
            displayLatenciesMode = false; 
            ROICreationMode = false;
            timelinesLoaded = false; 
            electodesLoaded = false;
            meshesLoaded = false;
            volumeLoaded = false; 
            collidersUpdated = false;
            hemiMeshesAvailables = false;
            whiteMeshesAvailables = false; 
            whiteInflatedMeshesAvailables = false;

             // loop check  
            updateGeometry = false;
            updateIRMF = false;
            updateImageCuts = false;
            updateCutPlanes = true;
            generatorIsComputing = false;
            generatorUpToDate = false;      
        }
}

    /// <summary>
    /// All objects displayed in the 3D scene
    /// </summary>
    public class DisplayedObjects3DView
    {        
        // Parents objects
        public GameObject MeshesParent = null; /**< meshes parent of the scene */
        public GameObject CutsParent = null; /**< cuts meshes parent of the scene */
        public GameObject PlotsParent = null;  /**< plots meshes parent of the scene */

        // lights
        public GameObject SharedDirLight = null; /**< shared light between all the scene cameras */

        // Lists of GameObjects to be displayed 
        public List<GameObject> MeshesList = null; /**< meshes of the scene */
        public List<GameObject> CutsList = null; /**< cut meshes of the scene */        
        public List<GameObject> MeshesListTriErasing = null; /**< tri erasing meshes of the scene */

    }
}