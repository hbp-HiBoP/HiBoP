using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HBP.Core.Enums;
using HBP.Core.Exceptions;
using HBP.Core.Data;

namespace HBP.Display.Module3D
{
    /// <summary>
    /// Class responsible for managing the MRIs of the scene
    /// </summary>
    /// <remarks>
    /// This class can load and store meshes for the corresponding scene.
    /// It is also used to select which MRI to display on the scene and to configure the calibration parameters of the selected MRI.
    /// </remarks>
    public class MRIManager : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Parent scene of the manager
        /// </summary>
        [SerializeField] private Base3DScene m_Scene;
        /// <summary>
        /// Component containing references to GameObjects of the 3D scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        /// <summary>
        /// List of the MRIs of the scene
        /// </summary>
        public List<Core.Object3D.MRI3D> MRIs { get; set; } = new List<Core.Object3D.MRI3D>();
        /// <summary>
        /// List of loaded MRIs
        /// </summary>
        public List<Core.Object3D.MRI3D> LoadedMRIs { get { return (from mri in MRIs where mri.IsLoaded select mri).ToList(); } }
        /// <summary>
        /// Selected MRI3D ID
        /// </summary>
        public int SelectedMRIID { get; private set; }
        /// <summary>
        /// Selected MRI3D
        /// </summary>
        public Core.Object3D.MRI3D SelectedMRI
        {
            get
            {
                return MRIs[SelectedMRIID];
            }
        }
        /// <summary>
        /// List of the preloaded MRIs of the scene
        /// </summary>
        public Dictionary<Patient, List<Core.Object3D.MRI3D>> PreloadedMRIs { get; set; } = new Dictionary<Patient, List<Core.Object3D.MRI3D>>();
        /// <summary>
        /// Min calibration factor (between 0 and 1)
        /// </summary>
        public float MRICalMinFactor { get; private set; } = 0.0f;
        /// <summary>
        /// Max calibration factor (between 0 and 1)
        /// </summary>
        public float MRICalMaxFactor { get; private set; } = 1.0f;
        
        /// <summary>
        /// Center of the loaded mri
        /// </summary>
        public Vector3 VolumeCenter { get; private set; } = new Vector3(0, 0, 0);
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a MRI to the MRI manager
        /// </summary>
        /// <param name="mri">MRI to be added</param>
        public void Add(MRI mri)
        {
            if (mri.IsUsable)
            {
                Core.Object3D.MRI3D mri3D = new Core.Object3D.MRI3D(mri, ApplicationState.UserPreferences.Data.Anatomic.MRIPreloading);
                if (ApplicationState.UserPreferences.Data.Anatomic.MRIPreloading)
                {
                    if (mri3D.IsLoaded)
                    {
                        MRIs.Add(mri3D);
                    }
                    else
                    {
                        throw new CanNotLoadNIIFile(mri.File);
                    }
                }
                else
                {
                    string name = !string.IsNullOrEmpty(m_Scene.Visualization.Configuration.MeshName) ? m_Scene.Visualization.Configuration.MeshName : m_Scene.Type == SceneType.SinglePatient ? "Preimplantation" : "MNI";
                    if (mri3D.Name == name) mri3D.Load();
                    MRIs.Add(mri3D);
                }
            }
        }
        /// <summary>
        /// Add a MRI to the MRI manager preloaded MRIs
        /// </summary>
        /// <param name="mri">MRI to be added</param>
        public void AddPreloaded(MRI mri, Patient patient)
        {
            if (mri.IsUsable)
            {
                if (!PreloadedMRIs.ContainsKey(patient)) PreloadedMRIs.Add(patient, new List<Core.Object3D.MRI3D>());
                PreloadedMRIs[patient].Add(new Core.Object3D.MRI3D(mri, true));
            }
        }
        /// <summary>
        /// Set the calibration values
        /// </summary>
        /// <param name="min">Min calibration value</param>
        /// <param name="max">Max calibration value</param>
        public void SetCalValues(float min, float max)
        {
            if (min != MRICalMinFactor || max != MRICalMaxFactor)
            {
                MRICalMinFactor = min;
                MRICalMaxFactor = max;
                m_Scene.SceneInformation.BaseCutTexturesNeedUpdate = true;
            }
        }
        /// <summary>
        /// Set the MRI to be used
        /// </summary>
        /// <param name="mriName">Name of the MRI to be used</param>
        public void Select(string mriName, bool onlyIfAlreadyLoaded = false)
        {
            int mriID = MRIs.FindIndex(m => m.Name == mriName);
            if (mriID == -1 || (onlyIfAlreadyLoaded && !MRIs[mriID].IsLoaded)) mriID = 0;

            SelectedMRIID = mriID;
            VolumeCenter = SelectedMRI.Volume.Center;
            m_Scene.SceneInformation.GeometryNeedsUpdate = true;
            m_Scene.ResetGenerators();
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Load every MRI that has not been loaded yet
        /// </summary>
        public void LoadMissing()
        {
            foreach (var mri in MRIs)
            {
                if (!mri.IsLoaded) mri.Load();
            }
        }
        #endregion
    }
}