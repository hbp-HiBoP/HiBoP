using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityStandardAssets.ImageEffects;

namespace HBP.Module3D
{
    public enum DisplayedItems { Meshes, Plots, ROI };

    public class Camera3D : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_AssociatedScene;
        private Column3D m_AssociatedColumn;
        private View3D m_AssociatedView;

        /// <summary>
        /// Camera component
        /// </summary>
        public Camera Camera { get; set; }

        [SerializeField, Candlelight.PropertyBackingField]
        private int m_CullingMask;
        public int CullingMask
        {
            get
            {
                return m_CullingMask;
            }
            set
            {
                m_CullingMask = value;
                Camera.cullingMask = m_CullingMask;
            }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private float m_StartDistance = 250.0f;
        public float StartDistance
        {
            get { return m_StartDistance; }
            set { m_StartDistance = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private float m_Speed = 50.0f;
        public float Speed
        {
            get { return m_Speed; }
            set { m_Speed = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private float m_ZoomSpeed = 3.5f;
        public float ZoomSpeed
        {
            get { return m_ZoomSpeed; }
            set { m_ZoomSpeed = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private float m_AutomaticRotationSpeed = 30.0f;
        public float AutomaticRotationSpeed
        {
            get { return m_AutomaticRotationSpeed; }
            set { m_AutomaticRotationSpeed = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private bool m_AutomaticRotation = false;
        public bool AutomaticRotation
        {
            get { return m_AutomaticRotation; }
            set { m_AutomaticRotation = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private float m_MinDistance = 50.0f;
        public float MinDistance
        {
            get { return m_MinDistance; }
            set { m_MinDistance = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private float m_MaxDistance = 750.0f;
        public float MaxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private Material m_XCircleMaterial;
        public Material XCircleMaterial
        {
            get { return m_XCircleMaterial; }
            set { m_XCircleMaterial = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private Material m_YCircleMaterial;
        public Material YCircleMaterial
        {
            get { return m_YCircleMaterial; }
            set { m_YCircleMaterial = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        private Material m_ZCircleMaterial;
        public Material ZCircleMaterial
        {
            get { return m_ZCircleMaterial; }
            set { m_ZCircleMaterial = value; }
        }
        
        public bool DisplayRotationCircles { get; set; }

        private float m_RotationCirclesRay = 300f;

        private Vector3 m_Target;
        public Vector3 LocalTarget
        {
            get
            {
                return m_Target;
            }
        }
        public Vector3 Target
        {
            get
            {
                return m_AssociatedView ? m_Target + m_AssociatedView.transform.position : m_Target;
            }
            set
            {
                m_Target = value;
            }
        }
        private Vector3 m_OriginalTarget;
        private Vector3 m_OriginalRotationEuler;

        private Vector3[] m_XRotationCircleVertices = null;
        private Vector3[] m_YRotationCircleVertices = null;
        private Vector3[] m_ZRotationCircleVertices = null;

        private List<Vector3[]> m_PlanesCutsCirclesVertices = new List<Vector3[]>();

        // Rendering Settings
        public AmbientMode AmbientMode = AmbientMode.Flat;
        public float AmbientIntensity = 1;
        public Color AmbiantLight = new Color(0.2f, 0.2f, 0.2f, 1);

        // post render
        [SerializeField]
        private Material m_PlaneMaterial;
        private bool m_DisplayCutsCircles = false;
        private double m_DisplayPlanesTimeRemaining = 1;
        private double m_DisplayPlanesTimeStart = 0;
        private double m_DisplayPlanesTimer = 0;
        #endregion

        #region Private Methods
        private void Awake()
        {
            Camera = GetComponent<Camera>();
            m_AssociatedScene = GetComponentInParent<Base3DScene>();
            m_AssociatedColumn = GetComponentInParent<Column3D>();
            m_AssociatedView = GetComponentInParent<View3D>();

            transform.localEulerAngles = new Vector3(0, 100, 90);
            m_OriginalRotationEuler = transform.localEulerAngles;
            m_StartDistance = Mathf.Clamp(m_StartDistance, m_MinDistance, m_MaxDistance);
            Target = m_AssociatedScene.ColumnManager.BothHemi.BoundingBox.Center;
            m_OriginalTarget = Target;
            transform.position = Target - transform.forward * m_StartDistance;
            GetComponent<EdgeDetection>().enabled = m_AssociatedScene.EdgeMode;

            // rotation circles
            m_XRotationCircleVertices = Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), m_RotationCirclesRay, 150);
            m_YRotationCircleVertices = Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), m_RotationCirclesRay, 150);
            m_ZRotationCircleVertices = Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), m_RotationCirclesRay, 150);
            for (int ii = 0; ii < m_XRotationCircleVertices.Length; ++ii)
            {
                m_XRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.up) * m_XRotationCircleVertices[ii];
                m_YRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.left) * m_YRotationCircleVertices[ii];
            }
        }
        private void Start()
        {
            m_AssociatedScene.Events.OnModifyPlanesCuts.AddListener(() =>
            {
                if (!m_AssociatedScene.SceneInformation.MRILoaded)
                    return;

                m_PlanesCutsCirclesVertices = new List<Vector3[]>();
                for (int ii = 0; ii < m_AssociatedScene.Cuts.Count; ++ii)
                {
                    Vector3 point = m_AssociatedScene.Cuts[ii].Point;
                    point.x *= -1;
                    point += m_AssociatedView.transform.position;
                    Vector3 normal = m_AssociatedScene.Cuts[ii].Normal;
                    normal.x *= -1;
                    Quaternion q = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);
                    m_PlanesCutsCirclesVertices.Add(Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), 100, 150));
                    for (int jj = 0; jj < 150; ++jj)
                    {
                        m_PlanesCutsCirclesVertices[ii][jj] = q * m_PlanesCutsCirclesVertices[ii][jj];
                        m_PlanesCutsCirclesVertices[ii][jj] += point;
                    }
                }
                m_DisplayPlanesTimeStart = (float)TimeExecution.GetWorldTime();
                m_DisplayPlanesTimer = 0;
                m_DisplayCutsCircles = true;
            });

            m_AssociatedScene.Events.OnUpdateCameraTarget.AddListener((target) =>
            {
                m_OriginalTarget = target + m_AssociatedView.transform.position;
                Target = target;
            });
        }
        private void OnPreCull()
        {
            RenderSettings.ambientMode = AmbientMode;
            RenderSettings.ambientIntensity = AmbientIntensity;
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = AmbiantLight;
            m_AssociatedScene.DisplayedObjects.SharedDirectionalLight.transform.eulerAngles = transform.eulerAngles;
        }
        private void OnPreRender()
        {
            if (m_AssociatedView.LineID == 0)
            {
                m_AssociatedScene.UpdateColumnRendering(m_AssociatedColumn);
            }
        }
        private void OnPostRender()
        {
            DrawGL();
        }
        private void Update()
        {
            if (m_AssociatedScene.IsSelected)
            {
                if (m_AssociatedView.IsSelected)
                {
                    Camera.backgroundColor = ApplicationState.Theme.Color.ClickedViewColor;
                }
                else if (m_AssociatedColumn.IsSelected)
                {
                    Camera.backgroundColor = ApplicationState.Theme.Color.SelectedViewColor;
                }
                else
                {
                    Camera.backgroundColor = ApplicationState.Theme.Color.RegularViewColor;
                }
            }
            else
            {
                Camera.backgroundColor = ApplicationState.Theme.Color.RegularViewColor;
            }
            AutomaticCameraRotation();
        }
        /// <summary>
        /// Make the camera rotate automatically
        /// </summary>
        private void AutomaticCameraRotation()
        {
            if (m_AutomaticRotation)
            {
                HorizontalRotation(m_AutomaticRotationSpeed * Time.deltaTime);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public void DrawGL()
        {
            if (m_AssociatedView.IsMinimized)
                return;

            if (m_DisplayCutsCircles)
            {
                m_DisplayPlanesTimer = TimeExecution.GetWorldTime() - m_DisplayPlanesTimeStart;
                if (m_DisplayPlanesTimeRemaining > m_DisplayPlanesTimer)
                {
                    m_PlaneMaterial.SetPass(0);
                    {
                        int ii = m_AssociatedScene.SceneInformation.LastPlaneModifiedID;
                        for (int jj = 0; jj < m_PlanesCutsCirclesVertices[ii].Length; ++jj)
                        {
                            GL.Begin(GL.LINES);
                            GL.Vertex(m_PlanesCutsCirclesVertices[ii][jj]);
                            GL.Vertex(m_PlanesCutsCirclesVertices[ii][(jj + 1) % m_PlanesCutsCirclesVertices[ii].Length]);
                            GL.End();
                        }

                        GL.Begin(GL.LINES);
                        GL.Vertex(m_PlanesCutsCirclesVertices[ii][m_PlanesCutsCirclesVertices[ii].Length / 8]);
                        GL.Vertex(m_PlanesCutsCirclesVertices[ii][5 * m_PlanesCutsCirclesVertices[ii].Length / 8]);
                        GL.End();
                        GL.Begin(GL.LINES);
                        GL.Vertex(m_PlanesCutsCirclesVertices[ii][3 * m_PlanesCutsCirclesVertices[ii].Length / 8]);
                        GL.Vertex(m_PlanesCutsCirclesVertices[ii][7 * m_PlanesCutsCirclesVertices[ii].Length / 8]);
                        GL.End();
                    }
                }
                else
                    m_DisplayCutsCircles = false;
            }

            if (DisplayRotationCircles)
            {
                //GL.PushMatrix();
                m_XCircleMaterial.SetPass(0);

                float currentDist = Vector3.Distance(transform.position, Target);
                float scaleRatio = currentDist / m_MaxDistance;

                for (int ii = 0; ii < m_XRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(Target + scaleRatio * m_XRotationCircleVertices[ii]);
                    GL.Vertex(Target + scaleRatio * m_XRotationCircleVertices[(ii + 1) % m_XRotationCircleVertices.Length]);
                    GL.End();
                }

                m_YCircleMaterial.SetPass(0);

                for (int ii = 0; ii < m_YRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(Target + scaleRatio * m_YRotationCircleVertices[ii]);
                    GL.Vertex(Target + scaleRatio * m_YRotationCircleVertices[(ii + 1) % m_YRotationCircleVertices.Length]);
                    GL.End();
                }

                m_ZCircleMaterial.SetPass(0);

                for (int ii = 0; ii < m_ZRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(Target + scaleRatio * m_ZRotationCircleVertices[ii]);
                    GL.Vertex(Target + scaleRatio * m_ZRotationCircleVertices[(ii + 1) % m_ZRotationCircleVertices.Length]);
                    GL.End();
                }
            }
        }
        /// <summary>
        /// Strafe hozizontally the camera position and target with the same vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        public void HorizontalStrafe(float amount)
        {
            Vector3 strafe = - transform.right * amount;

            transform.position = transform.position + strafe;
            Target = Target + strafe;
        }
        /// <summary>
        /// Strafe vertically the camera position and target with the same vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        public void VerticalStrafe(float amount)
        {
            Vector3 strafe = - transform.up * amount;

            transform.position = transform.position + strafe;
            Target = Target + strafe;
        }
        /// <summary>
        /// Turn horizontally around the camera target
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        public void HorizontalRotation(float amount)
        {
            Vector3 vecTargetPos_EyePos = transform.position - Target;
            Quaternion rotation = Quaternion.AngleAxis(amount, transform.up);

            transform.position = rotation * vecTargetPos_EyePos + Target;
            transform.LookAt(Target, transform.up);
        }
        /// <summary>
        /// Turn vertically around the camera target
        /// </summary>
        /// <param name="up"></param>
        /// <param name="amount"></param>
        public void VerticalRotation(float amount)
        {
            Vector3 vecTargetPos_EyePos = transform.position - Target;
            Quaternion rotation = Quaternion.AngleAxis(-amount, transform.right);

            transform.position = rotation * vecTargetPos_EyePos + Target;
            transform.LookAt(Target, Vector3.Cross(Target - transform.position, transform.right));
        }
        /// <summary>
        /// Zoom towards target
        /// </summary>
        /// <param name="amount">Distance</param>
        public void Zoom(float amount)
        {
            float distance = Vector3.Distance(transform.position, Target) - amount;
            if (distance > m_MinDistance && distance < m_MaxDistance)
            {
                transform.position += transform.forward * amount;
            }
            else if (distance >= m_MaxDistance)
            {
                transform.position = Target - transform.forward * m_MaxDistance;
            }
            else if (distance <= m_MinDistance)
            {
                transform.position = Target - transform.forward * m_MinDistance;
            }
        }
        /// <summary>
        /// Reset the original target of the camera
        /// </summary>
        public void ResetTarget()
        {
            transform.localEulerAngles = m_OriginalRotationEuler;
            Target = m_OriginalTarget;
            transform.position = Target - transform.forward * m_StartDistance;
        }
        #endregion
    }
}