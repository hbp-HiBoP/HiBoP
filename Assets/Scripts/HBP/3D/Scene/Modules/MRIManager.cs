using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Module3D
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
        public List<MRI3D> MRIs = new List<MRI3D>();
        /// <summary>
        /// List of loaded MRIs
        /// </summary>
        public List<MRI3D> LoadedMRIs { get { return (from mri in MRIs where mri.IsLoaded select mri).ToList(); } }
        /// <summary>
        /// Selected MRI3D ID
        /// </summary>
        public int SelectedMRIID { get; private set; }
        /// <summary>
        /// Selected MRI3D
        /// </summary>
        public MRI3D SelectedMRI
        {
            get
            {
                return MRIs[SelectedMRIID];
            }
        }
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

        #region Private Methods
        private void OnDestroy()
        {
            foreach (var mri in MRIs)
            {
                if (!mri.HasBeenLoadedOutside)
                {
                    mri.Clean();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a MRI to the MRI manager
        /// </summary>
        /// <param name="mri">MRI to be added</param>
        public void Add(Data.MRI mri)
        {
            if (mri.IsUsable)
            {
                MRI3D mri3D = new MRI3D(mri);
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
                    string name = !string.IsNullOrEmpty(m_Scene.Visualization.Configuration.MeshName) ? m_Scene.Visualization.Configuration.MeshName : m_Scene.Type == Data.Enums.SceneType.SinglePatient ? "Preimplantation" : "MNI";
                    if (mri3D.Name == name) mri3D.Load();
                    MRIs.Add(mri3D);
                }
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
                m_Scene.CutTexturesNeedUpdate = true;
            }
        }
        /// <summary>
        /// Set the MRI to be used
        /// </summary>
        /// <param name="mriName">Name of the MRI to be used</param>
        public void Select(string mriName)
        {
            int mriID = MRIs.FindIndex(m => m.Name == mriName);
            if (mriID == -1) mriID = 0;

            SelectedMRIID = mriID;
            VolumeCenter = SelectedMRI.Volume.Center;
            DLL.MRIVolumeGenerator baseGenerator = new DLL.MRIVolumeGenerator();
            baseGenerator.Reset(SelectedMRI.Volume, 120);
            foreach (var column in m_Scene.ColumnsDynamic)
            {
                column.DLLMRIVolumeGenerator = new DLL.MRIVolumeGenerator(baseGenerator);
            }
            m_Scene.MeshGeometryNeedsUpdate = true;
            m_Scene.ResetIEEG();
            foreach (Column3D column in m_Scene.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
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