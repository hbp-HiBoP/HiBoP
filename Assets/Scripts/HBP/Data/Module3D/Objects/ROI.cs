using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using HBP.Core.Interfaces;
using HBP.Core.Tools;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Class containing information about a Region Of Interest in the scene
    /// </summary>
    public class ROI : MonoBehaviour, IIdentifiable
    {
        private string m_ID;

        public string ID
        {
            get => m_ID ??= Guid.NewGuid().ToString();
            set => m_ID = value;
        }

        public void GenerateID() => ID = Guid.NewGuid().ToString();

        #region Properties

        private string m_Name = "ROI";

        /// <summary>
        /// Name of the ROI
        /// </summary>
        public string Name
        {
            get { return m_Name; }
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
        public int SelectedSphereID { get; set; } = -1;

        /// <summary>
        /// Currently selected sphere of this ROI
        /// </summary>
        public Sphere SelectedSphere
        {
            get { return SelectedSphereID < 0 || SelectedSphereID >= Spheres.Count ? null : Spheres[SelectedSphereID]; }
        }

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
        public UnityEvent OnUpdateROIName = new();

        /// <summary>
        /// Event called when adding of removing a sphere in this ROI
        /// </summary>
        public UnityEvent OnChangeNumberOfSpheres = new();

        /// <summary>
        /// Event called when modifying a sphere of this ROI
        /// </summary>
        public UnityEvent OnChangeSphereParameters = new();

        public GenericEvent<Sphere> OnSphereDefinitionChanged { get; } = new();
        private bool applyingDefinition;

        /// <summary>
        /// Event called when selecting or deselecting a sphere
        /// </summary>
        public UnityEvent OnChangeSphereSelectionState = new();

        #endregion

        #region Private Methods

        void Awake()
        {
            SelectedSphereID = -1;
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
        /// <param name="sites">List of the sites of the scene</param>
        /// <param name="mask">ROI mask for the sites (true if a site is not in this ROI)</param>
        public void UpdateMask(IReadOnlyList<Core.Object3D.Site> sites, bool[] mask)
        {
            for (int ii = 0; ii < sites.Count; ++ii)
            {
                mask[ii] = !Contains(sites[ii].Information.DefaultPosition);
            }
        }

        /// <summary>
        /// Return true if the input position is inside this ROI.
        /// </summary>
        /// <param name="position">Position to test in Unity space</param>
        /// <returns>True if the position is inside at least one ROI sphere</returns>
        public bool Contains(Vector3 position)
        {
            for (int ii = 0; ii < Spheres.Count; ++ii)
            {
                if (Spheres[ii].Contains(position))
                {
                    return true;
                }
            }

            return false;
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
                    Vector3 vec = new(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
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
            if (sphereID < -1 || sphereID >= Spheres.Count || SelectedSphereID == sphereID) return;
            if (SelectedSphere != null) SelectedSphere.Selected = false;
            SelectedSphereID = sphereID;
            if (SelectedSphere != null) SelectedSphere.Selected = true;
            OnChangeSphereSelectionState.Invoke();
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
            Sphere sphere = Instantiate(m_SpherePrefab, transform).GetComponent<Sphere>();
            sphere.Initialize(m_Layer, name, radius, position);
            sphere.OnDefinitionChanged.AddListener(() =>
            {
                if (!applyingDefinition) NotifySphereDefinition(sphere);
            });
            Spheres.Add(sphere);

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
                SetSphereDefinition(SelectedSphere, SelectedSphere.Position + translation, SelectedSphere.InfluenceRadius);
            }
        }

        /// <summary>
        /// Remove a sphere from this ROI given its index
        /// </summary>
        /// <param name="sphereID">Index of the sphere to be removed</param>
        public void RemoveSphere(int sphereID)
        {
            if (sphereID < 0 || sphereID >= Spheres.Count) return;

            Sphere selected = SelectedSphere;

            // remove the sphere
            if (Application.isPlaying) Destroy(Spheres[sphereID].gameObject);
            else DestroyImmediate(Spheres[sphereID].gameObject);
            Spheres.RemoveAt(sphereID);

            // Repair selection before observers inspect the new roster.
            SelectedSphereID = -1;
            SelectSphere(selected != null && Spheres.Contains(selected) ? Spheres.IndexOf(selected) : Mathf.Min(sphereID, Spheres.Count - 1));
            OnChangeNumberOfSpheres.Invoke();
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
                SetSphereDefinition(SelectedSphere, SelectedSphere.Position, SelectedSphere.InfluenceRadius * (direction < 0 ? 0.9f : 1.1f));
            }
        }

        public bool SetSphereDefinition(Sphere sphere, Vector3 position, float radius)
        {
            if (sphere == null || !Spheres.Contains(sphere)) throw new ArgumentException("The sphere is not part of this ROI.", nameof(sphere));
            bool changed;
            applyingDefinition = true;
            try
            {
                changed = sphere.SetDefinition(position, radius);
            }
            finally
            {
                applyingDefinition = false;
            }

            if (changed) NotifySphereDefinition(sphere);
            return changed;
        }

        private void NotifySphereDefinition(Sphere sphere)
        {
            OnSphereDefinitionChanged.Invoke(sphere);
            OnChangeSphereParameters.Invoke();
        }

        #endregion
    }
}
