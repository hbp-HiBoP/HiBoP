using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Core.Object3D
{
    /// <summary>
    /// Class containing information about a Region Of Interest in the scene
    /// </summary>
    public class ROI : MonoBehaviour
    {
        #region Properties
        private string m_Name = "ROI";
        /// <summary>
        /// Name of the ROI
        /// </summary>
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
                OnUpdateROIName.Invoke();
            }
        }
        /// <summary>
        /// Layer on which the ROI will be displayed
        /// </summary>
        private int m_Layer;

        /// <summary>
        /// Index of the selected sphere of this ROI
        /// </summary>
        public int SelectedSphereID { get; set; }
        /// <summary>
        /// Currently selected sphere of this ROI
        /// </summary>
        public Sphere SelectedSphere
        {
            get
            {
                return SelectedSphereID == -1 ? null : Spheres[SelectedSphereID];
            }
        }

        /// <summary>
        /// Pointer to the DLL object corresponding to this ROI
        /// </summary>
        private DLL.ROI m_DLLROI;
        /// <summary>
        /// List of this spheres of this ROI
        /// </summary>
        public List<Sphere> Spheres { get; private set; } = new List<Sphere>();

        /// <summary>
        /// Prefab for the sphere game object
        /// </summary>
        [SerializeField] private GameObject m_SpherePrefab;
        #endregion

        #region Events
        /// <summary>
        /// Event called when updating the name of the ROI
        /// </summary>
        public UnityEvent OnUpdateROIName = new UnityEvent();
        /// <summary>
        /// Event called when adding of removing a sphere in this ROI
        /// </summary>
        public UnityEvent OnChangeNumberOfSpheres = new UnityEvent();
        /// <summary>
        /// Event called when modifying a sphere of this ROI
        /// </summary>
        public UnityEvent OnChangeSphereParameters = new UnityEvent();
        #endregion

        #region Private Methods
        void Awake()
        {
            m_DLLROI = new DLL.ROI();
            SelectedSphereID = -1;
        }
        private void OnDestroy()
        {
            m_DLLROI?.Dispose();
        }
        /// <summary>
        /// Unselect the currently selected sphere of this ROI
        /// </summary>
        private void UnselectSphere()
        {
            if (SelectedSphereID != -1 && SelectedSphereID < Spheres.Count)
            {
                Spheres[SelectedSphereID].Selected = false;
            }
            SelectedSphereID = -1;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Display or hide all spheres of this ROI
        /// </summary>
        /// <param name="visibility">True if the spheres will become visible</param>
        public void SetVisibility(bool visibility)
        {
            for (int ii = 0; ii < Spheres.Count; ++ii)
            {
                Spheres[ii].gameObject.SetActive(visibility);
                if (visibility)
                {
                    Spheres[ii].StartAnimation();
                }
            }
        }
        /// <summary>
        /// Enable or disable the rendering of the spheres of this ROI
        /// </summary>
        /// <param name="state">True if spheres are supposed to be visible</param>
        public void SetRenderingState(bool state)
        {
            int inactiveLayer = LayerMask.NameToLayer("Inactive");
            for (int ii = 0; ii < Spheres.Count; ++ii)
            {
                Spheres[ii].gameObject.layer = (state ? m_Layer : inactiveLayer);
            }
        }
        /// <summary>
        /// Update the ROI mask of the sites using this ROI
        /// </summary>
        /// <param name="plots">Raw list of the sites of the scene</param>
        /// <param name="mask">ROI mask for the sites (true if a site is not in this ROI)</param>
        public void UpdateMask(DLL.RawSiteList plots, bool[] mask)
        {
            m_DLLROI.UpdateMask(plots, mask);
        }
        /// <summary>
        /// Select the closest sphere from a raycast
        /// </summary>
        /// <param name="ray">Ray of the raycast</param>
        public void SelectClosestSphere(Ray ray)
        {
            int minDistId = -1;
            float minDist = float.MaxValue;

            for (int ii = 0; ii < Spheres.Count; ++ii)
            {
                if (Spheres[ii].CheckCollision(ray, out RaycastHit hitInfo))
                {
                    Vector3 p1 = hitInfo.point;
                    Vector3 p2 = ray.origin;
                    Vector3 vec = new Vector3(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
                    float squareDist = vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2];

                    if (squareDist < minDist)
                    {
                        minDist = squareDist;
                        minDistId = ii;
                    }
                }
            }

            SelectSphere(minDistId);
        }
        /// <summary>
        /// Select a sphere of this ROI given its index
        /// </summary>
        /// <param name="sphereID">Index of the sphere to be selected</param>
        public void SelectSphere(int sphereID)
        {
            if (sphereID >= Spheres.Count)
                return;

            UnselectSphere();

            if (sphereID >= 0)
            {
                Spheres[sphereID].Selected = true;
                SelectedSphereID = sphereID;
            }
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Add a new sphere to this ROI
        /// </summary>
        /// <param name="layer">Layer of the sphere (corresponds to the layer of the column)</param>
        /// <param name="name">Name of the sphere</param>
        /// <param name="position">Position of the sphere</param>
        /// <param name="radius">Radius of the sphere</param>
        public void AddSphere(string layer, string name, Vector3 position, float radius)
        {
            m_Layer = LayerMask.NameToLayer(layer);
            HBP.Module3D.Sphere sphere = Instantiate(m_SpherePrefab, transform).GetComponent<Sphere>();
            sphere.Initialize(m_Layer, name, radius, position);
            sphere.OnChangeRadius.AddListener(() =>
            {
                OnChangeSphereParameters.Invoke();
            });
            Spheres.Add(sphere);

            // DLL
            Vector3 positionSphere = sphere.transform.localPosition;
            positionSphere.x = -positionSphere.x;
            m_DLLROI.AddSphere(radius, positionSphere);

            OnChangeNumberOfSpheres.Invoke();
            SelectSphere(Spheres.Count - 1);
        }
        /// <summary>
        /// Move the selected sphere by a specific amount
        /// </summary>
        /// <param name="translation">Amount to move the sphere</param>
        public void MoveSelectedSphere(Vector3 translation)
        {
            if (SelectedSphereID != -1)
            {
                SelectedSphere.Position += translation;

                // DLL
                Vector3 positionSphere = SelectedSphere.Position;
                positionSphere.x = -positionSphere.x;
                m_DLLROI.UpdateSpherePosition(SelectedSphereID, positionSphere);

                OnChangeSphereParameters.Invoke();
            }
        }
        /// <summary>
        /// Remove a sphere from this ROI given its index
        /// </summary>
        /// <param name="sphereID">Index of the sphere to be removed</param>
        public void RemoveSphere(int sphereID)
        {
            if (sphereID == -1) return;

            // remove the sphere
            Destroy(Spheres[sphereID].gameObject);
            Spheres.RemoveAt(sphereID);

            // remove dll sphere
            m_DLLROI.RemoveSphere(sphereID);

            OnChangeNumberOfSpheres.Invoke();
            
            if (SelectedSphereID - 1 == -1 && Spheres.Count > 0)
            {
                SelectSphere(SelectedSphereID);
            }
            else
            {
                SelectSphere(SelectedSphereID - 1);
            }
        }
        /// <summary>
        /// Remove the currently selected sphere
        /// </summary>
        public void RemoveSelectedSphere()
        {
            RemoveSphere(SelectedSphereID);
        }
        /// <summary>
        /// Increase or decrease the size of the selected sphere by 10%
        /// </summary>
        /// <param name="direction">If negative, decrease the size of the sphere (direction must have an amplitude greater than 0.2 to be accounted for)</param>
        public void ChangeSelectedSphereSize(float direction)
        {
            if (SelectedSphereID == -1) return;

            if (Mathf.Abs(direction) > 0.2f)
            {
                SelectedSphere.Radius *= direction < 0 ? 0.9f : 1.1f;

                // DLL
                m_DLLROI.UpdateSphereRadius(SelectedSphereID, SelectedSphere.Radius);

                OnChangeSphereParameters.Invoke();
            }
        }
        #endregion
    }
}