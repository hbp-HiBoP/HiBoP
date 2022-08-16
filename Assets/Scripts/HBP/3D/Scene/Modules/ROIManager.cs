using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Display.Module3D
{
    public class ROIManager : MonoBehaviour
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
        /// List of the ROIs of this column
        /// </summary>
        public List<Core.Object3D.ROI> ROIs { get; protected set; } = new List<Core.Object3D.ROI>();

        protected Core.Object3D.ROI m_SelectedROI = null;
        /// <summary>
        /// Currently selected ROI
        /// </summary>
        public Core.Object3D.ROI SelectedROI
        {
            get
            {
                return m_SelectedROI;
            }
            set
            {
                if (m_SelectedROI != null)
                {
                    m_SelectedROI.SetVisibility(false);
                }

                if (value == null)
                {
                    m_SelectedROI = null;
                }
                else
                {
                    m_SelectedROI = value;
                    m_SelectedROI.SetVisibility(true);
                }

                UpdateROIMasks();
            }
        }
        /// <summary>
        /// ID of the currently selected ROI
        /// </summary>
        public int SelectedROIID
        {
            get
            {
                return ROIs.FindIndex((roi) => roi == SelectedROI);
            }
            set
            {
                SelectedROI = value == -1 ? null : ROIs[value];
            }
        }
        
        private bool m_ROICreationMode;
        /// <summary>
        /// Is ROI creation mode activated ?
        /// </summary>
        public bool ROICreationMode
        {
            get
            {
                return m_ROICreationMode;
            }
            set
            {
                m_ROICreationMode = value;
                if (SelectedROI != null)
                    SelectedROI.SetRenderingState(value);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a ROI to this column
        /// </summary>
        /// <param name="name">Name of the new ROI</param>
        /// <returns>Newly created ROI</returns>
        public Core.Object3D.ROI AddROI(string name = "ROI")
        {
            Core.Object3D.ROI roi = m_DisplayedObjects.InstantiateROI();
            roi.Name = name;
            roi.OnUpdateROIName.AddListener(() =>
            {
                m_Scene.OnUpdateROI.Invoke();
                HBP3DModule.OnRequestUpdateInToolbar.Invoke();
            });
            roi.OnChangeNumberOfSpheres.AddListener(() =>
            {
                UpdateROIMasks();
            });
            roi.OnChangeSphereParameters.AddListener(() =>
            {
                UpdateROIMasks();
            });
            roi.OnChangeSphereSelectionState.AddListener(() =>
            {
                HBP3DModule.OnRequestUpdateInToolbar.Invoke();
            });
            ROIs.Add(roi);
            UpdateROIMasks();
            SelectedROI = ROIs.Last();

            return roi;
        }
        /// <summary>
        /// Create a new ROI using the parameters of another ROI
        /// </summary>
        /// <param name="roi">ROI to copy parameters from</param>
        public void CopyROI(Core.Object3D.ROI roi)
        {
            Core.Object3D.ROI newROI = AddROI();
            newROI.Name = roi.Name;
            foreach (Core.Object3D.Sphere sphere in roi.Spheres)
            {
                newROI.AddSphere(HBP3DModule.DEFAULT_MESHES_LAYER, "Sphere", sphere.Position, sphere.Radius);
            }
        }
        /// <summary>
        /// Remove the currently selected ROI
        /// </summary>
        public void RemoveSelectedROI()
        {
            Destroy(m_SelectedROI.gameObject);
            ROIs.Remove(m_SelectedROI);
            UpdateROIMasks();

            if (ROIs.Count > 0)
            {
                SelectedROI = ROIs.Last();
            }
            else
            {
                SelectedROI = null;
            }
        }
        /// <summary>
        /// Remove all ROIs of the manager
        /// </summary>
        public void Clear()
        {
            foreach (var roi in ROIs)
            {
                Destroy(roi.gameObject);
            }
            ROIs.Clear();
            UpdateROIMasks();
            SelectedROI = null;
        }
        /// <summary>
        /// Move the selected sphere by a specific delta from a camera perspective
        /// </summary>
        /// <param name="camera">Reference camera</param>
        /// <param name="delta">Distance and direction of the movement</param>
        public void MoveSelectedROISphere(Camera camera, Vector3 delta)
        {
            if (m_SelectedROI)
            {
                if (m_SelectedROI.SelectedSphereID != -1)
                {
                    Vector3 position = camera.WorldToScreenPoint(m_SelectedROI.SelectedSphere.transform.position);
                    position += delta;
                    position = camera.ScreenToWorldPoint(position);
                    position -= m_SelectedROI.SelectedSphere.transform.position;
                    m_SelectedROI.MoveSelectedSphere(position);
                }
            }
        }
        /// <summary>
        /// Update the ROI mask for this column
        /// </summary>
        public void UpdateROIMasks()
        {
            if (SelectedROI == null)
            {
                foreach (var column in m_Scene.Columns)
                {
                    for (int ii = 0; ii < column.Sites.Count; ++ii)
                        column.Sites[ii].State.IsOutOfROI = true;
                }
            }
            else
            {
                foreach (var column in m_Scene.Columns)
                {
                    bool[] maskROI = new bool[column.Sites.Count];

                    // update mask ROI
                    for (int ii = 0; ii < maskROI.Length; ++ii)
                        maskROI[ii] = column.Sites[ii].State.IsOutOfROI;

                    SelectedROI.UpdateMask(column.RawElectrodes, maskROI);
                    for (int ii = 0; ii < column.Sites.Count; ++ii)
                        column.Sites[ii].State.IsOutOfROI = maskROI[ii];
                }
            }
            m_Scene.ResetGenerators(false);
            m_Scene.OnUpdateROI.Invoke();
            HBP3DModule.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Load the ROIs from the visualization configuration
        /// </summary>
        /// <param name="rois">List of the ROIs in the configuration</param>
        public void LoadROIsFromConfiguration(IEnumerable<Core.Data.RegionOfInterest> rois)
        {
            ROICreationMode = !ROICreationMode;
            foreach (Core.Data.RegionOfInterest roi in rois)
            {
                Core.Object3D.ROI newROI = AddROI(roi.Name);
                foreach (Core.Data.Sphere sphere in roi.Spheres)
                {
                    newROI.AddSphere(HBP3DModule.DEFAULT_MESHES_LAYER, "Sphere", sphere.Position.ToVector3(), sphere.Radius);
                }
            }
            ROICreationMode = !ROICreationMode;
        }
        #endregion
    }
}
