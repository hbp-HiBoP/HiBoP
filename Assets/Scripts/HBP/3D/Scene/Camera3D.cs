using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using ThirdParty.ImageEffects;
using HBP.Core.Enums;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Camera associated to a view 3D
    /// </summary>
    public class Camera3D : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Scene associated to this camera
        /// </summary>
        private Base3DScene m_AssociatedScene;
        /// <summary>
        /// Column associated to this camera
        /// </summary>
        private Column3D m_AssociatedColumn;
        /// <summary>
        /// View associated to this camera
        /// </summary>
        private View3D m_AssociatedView;

        [SerializeField] private Camera m_Camera;
        /// <summary>
        /// Camera component
        /// </summary>
        public Camera Camera { get { return m_Camera; } }

        /// <summary>
        /// Culling mask of the camera (so it doesn't render when view is minimized)
        /// </summary>
        public int CullingMask
        {
            get
            {
                return m_Camera.cullingMask;
            }
            set
            {
                m_Camera.cullingMask = value;
            }
        }
        /// <summary>
        /// Automatic rotation speed
        /// </summary>
        public float AutomaticRotationSpeed { get; set; }
        /// <summary>
        /// Is the camera automatically rotating ?
        /// </summary>
        public bool AutomaticRotation { get; set; }

        /// <summary>
        /// Minimum distance between camera and target
        /// </summary>
        [SerializeField] private float m_MinDistance = 50.0f;
        /// <summary>
        /// Maximum distance between camera and target
        /// </summary>
        [SerializeField] private float m_MaxDistance = 750.0f;
        /// <summary>
        /// Starting distance between camera and target
        /// </summary>
        [SerializeField] private float m_StartDistance = 250.0f;
        /// <summary>
        /// Current distance between camera and target
        /// </summary>
        private float m_Distance { get { return Vector3.Distance(transform.position, Target); } }

        /// <summary>
        /// Rotation speed when dragging
        /// </summary>
        [SerializeField] private float m_Speed = 1.0f;
        /// <summary>
        /// Zoom speed of the camera
        /// </summary>
        [SerializeField] private float m_ZoomSpeed = 1.0f;
        
        /// <summary>
        /// Do we display the rotation circles ?
        /// </summary>
        public bool DisplayRotationCircles { get; set; }
        /// <summary>
        /// Radius of the rotation circles
        /// </summary>
        [SerializeField] private float m_RotationCirclesRadius = 300f;

        /// <summary>
        /// Material of the X rotation circle
        /// </summary>
        [SerializeField] private Material m_XCircleMaterial;
        /// <summary>
        /// Material of the Y rotation circle
        /// </summary>
        [SerializeField] private Material m_YCircleMaterial;
        /// <summary>
        /// Material of the Z rotation circle
        /// </summary>
        [SerializeField] private Material m_ZCircleMaterial;

        private CameraControl m_Type = CameraControl.Trackball;
        /// <summary>
        /// Type of the rotation
        /// </summary>
        public CameraControl Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                m_Type = value;
                m_AssociatedView.Default();
            }
        }
        
        /// <summary>
        /// Local target position
        /// </summary>
        public Vector3 LocalTarget { get; private set; }
        /// <summary>
        /// Global target position
        /// </summary>
        public Vector3 Target
        {
            get
            {
                return m_AssociatedView ? LocalTarget + m_AssociatedView.transform.position : LocalTarget;
            }
            set
            {
                LocalTarget = value;
            }
        }
        /// <summary>
        /// Original target position
        /// </summary>
        private Vector3 m_OriginalTarget;
        /// <summary>
        /// Original camera rotation angle
        /// </summary>
        private Vector3 m_OriginalRotationEuler;

        /// <summary>
        /// Vertices of the X rotation circle
        /// </summary>
        private Vector3[] m_XRotationCircleVertices;
        /// <summary>
        /// Vertices of the Y rotation circle
        /// </summary>
        private Vector3[] m_YRotationCircleVertices;
        /// <summary>
        /// Vertices of the Z rotation circle
        /// </summary>
        private Vector3[] m_ZRotationCircleVertices;

        /// <summary>
        /// Vertices of the plane cut circle
        /// </summary>
        private List<Vector3[]> m_PlanesCutsCirclesVertices = new List<Vector3[]>();
        
        /// <summary>
        /// Material of the cuts
        /// </summary>
        [SerializeField] private Material m_PlaneMaterial;
        /// <summary>
        /// Do we display cut circle ?
        /// </summary>
        private bool m_DisplayCutsCircles = false;
        /// <summary>
        /// Time before the cut circle disappear
        /// </summary>
        private float m_DisplayPlanesTimeRemaining = 1f;
        /// <summary>
        /// Time since we begin to display circles
        /// </summary>
        private float m_DisplayPlanesTimer = 0f;

        /// <summary>
        /// Ambient mode for the camera
        /// </summary>
        public AmbientMode AmbientMode = AmbientMode.Flat;
        /// <summary>
        /// Ambient intensity
        /// </summary>
        public float AmbientIntensity = 1;
        /// <summary>
        /// Ambient light color
        /// </summary>
        public Color AmbientLight = new Color(0.2f, 0.2f, 0.2f, 1);

        /// <summary>
        /// State for the selected view
        /// </summary>
        [SerializeField] Theme.State Selected;
        /// <summary>
        /// State for the clicked view
        /// </summary>
        [SerializeField] Theme.State Clicked;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_AssociatedScene = GetComponentInParent<Base3DScene>();
            m_AssociatedColumn = GetComponentInParent<Column3D>();
            m_AssociatedView = GetComponentInParent<View3D>();

            transform.localEulerAngles = new Vector3(0, 100, 90);
            m_OriginalRotationEuler = transform.localEulerAngles;
            m_StartDistance = Mathf.Clamp(m_StartDistance, m_MinDistance, m_MaxDistance);
            Target = m_AssociatedScene.MeshManager.SelectedMesh.Both.Center;
            m_OriginalTarget = LocalTarget;
            transform.position = Target - transform.forward * m_StartDistance;

            GetComponent<EdgeDetection>().enabled = m_AssociatedScene.EdgeMode;
            AutomaticRotation = m_AssociatedScene.AutomaticRotation;
            AutomaticRotationSpeed = m_AssociatedScene.AutomaticRotationSpeed;

            // rotation circles
            m_XRotationCircleVertices = Core.Object3D.Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), m_RotationCirclesRadius, 150);
            m_YRotationCircleVertices = Core.Object3D.Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), m_RotationCirclesRadius, 150);
            m_ZRotationCircleVertices = Core.Object3D.Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), m_RotationCirclesRadius, 150);
            for (int ii = 0; ii < m_XRotationCircleVertices.Length; ++ii)
            {
                m_XRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.up) * m_XRotationCircleVertices[ii];
                m_YRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.left) * m_YRotationCircleVertices[ii];
            }

            m_AssociatedScene.OnModifyPlanesCuts.AddListener(() =>
            {
                m_PlanesCutsCirclesVertices = new List<Vector3[]>();
                for (int ii = 0; ii < m_AssociatedScene.Cuts.Count; ++ii)
                {
                    Vector3 point = m_AssociatedScene.Cuts[ii].Point;
                    point.x *= -1;
                    point += m_AssociatedView.transform.position;
                    Vector3 normal = m_AssociatedScene.Cuts[ii].Normal;
                    normal.x *= -1;
                    Quaternion q = Quaternion.FromToRotation(new Vector3(0, 0, 1), normal);
                    m_PlanesCutsCirclesVertices.Add(Core.Object3D.Geometry.Create3DCirclePoints(new Vector3(0, 0, 0), 100, 150));
                    for (int jj = 0; jj < 150; ++jj)
                    {
                        m_PlanesCutsCirclesVertices[ii][jj] = q * m_PlanesCutsCirclesVertices[ii][jj];
                        m_PlanesCutsCirclesVertices[ii][jj] += point;
                    }
                }
                m_DisplayPlanesTimer = 0;
                m_DisplayCutsCircles = true;
            });

            m_AssociatedScene.OnUpdateCameraTarget.AddListener((target) =>
            {
                Vector3 translation = (target - m_OriginalTarget);
                transform.localPosition += translation;
                Target = LocalTarget + translation;
                m_OriginalTarget = target;
            });
        }
        private void OnPreCull()
        {
            RenderSettings.ambientMode = AmbientMode;
            RenderSettings.ambientIntensity = AmbientIntensity;
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = AmbientLight;
            Module3DMain.SharedDirectionalLight.transform.eulerAngles = transform.eulerAngles;
            Module3DMain.SharedSpotlight.transform.eulerAngles = transform.eulerAngles;
            Module3DMain.SharedSpotlight.transform.position = transform.position;
        }
        private void OnPreRender()
        {
            m_AssociatedScene.UpdateColumnRendering(m_AssociatedColumn);
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
                    m_Camera.GetComponent<Theme.Components.ThemeElement>().Set(Clicked);
                }
                else if (m_AssociatedColumn.IsSelected)
                {
                    m_Camera.GetComponent<Theme.Components.ThemeElement>().Set(Selected);
                }
                else
                {
                    m_Camera.GetComponent<Theme.Components.ThemeElement>().Set();
                }
            }
            else
            {
                m_Camera.GetComponent<Theme.Components.ThemeElement>().Set();
            }
            AutomaticCameraRotation();
        }
        /// <summary>
        /// Make the camera rotate automatically
        /// </summary>
        private void AutomaticCameraRotation()
        {
            if (AutomaticRotation)
            {
                HorizontalRotation(AutomaticRotationSpeed * Time.deltaTime);
            }
        }
        /// <summary>
        /// Draw the GL objects (cut and rotation circles)
        /// </summary>
        private void DrawGL()
        {
            if (m_AssociatedView.IsMinimized)
                return;

            if (m_DisplayCutsCircles)
            {
                if (m_DisplayPlanesTimer < m_DisplayPlanesTimeRemaining)
                {
                    m_DisplayPlanesTimer += Time.deltaTime;
                    m_PlaneMaterial.SetPass(0);
                    {
                        int ii = m_AssociatedScene.LastPlaneModifiedIndex;
                        if (ii < m_PlanesCutsCirclesVertices.Count)
                        {
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
                }
                else
                    m_DisplayCutsCircles = false;
            }

            if (DisplayRotationCircles)
            {
                //GL.PushMatrix();
                m_XCircleMaterial.SetPass(0);

                float scaleRatio = m_Distance / m_MaxDistance;

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
        #endregion

        #region Public Methods
        /// <summary>
        /// Strafe the camera horizontally (keeping the same camera direction)
        /// </summary>
        /// <param name="amount">Strafe distance</param>
        public void HorizontalStrafe(float amount)
        {
            Vector3 strafe = amount * m_Speed * 0.2f * - transform.right;

            transform.position = transform.position + strafe;
            LocalTarget += strafe;
        }
        /// <summary>
        /// Strafe the camera vertically (keeping the same camera direction)
        /// </summary>
        /// <param name="amount">Strafe distance</param>
        public void VerticalStrafe(float amount)
        {
            Vector3 strafe = amount * m_Speed * - transform.up;

            transform.position = transform.position + strafe;
            LocalTarget += strafe;
        }
        /// <summary>
        /// Rotate the camera horizontally
        /// </summary>
        /// <param name="amount">Rotation amount</param>
        public void HorizontalRotation(float amount)
        {
            switch (Type)
            {
                case CameraControl.Trackball:
                    transform.RotateAround(Target, transform.up, amount * m_Speed);
                    break;
                case CameraControl.Orbital:
                    int direction = Vector3.Dot(transform.up, Vector3.forward) > 0 ? 1 : -1;
                    transform.RotateAround(Target, Vector3.forward, direction * amount * m_Speed);
                    break;
                default:
                    transform.RotateAround(Target, transform.up, amount * m_Speed);
                    break;
            }
        }
        /// <summary>
        /// Rotate the camera vertically
        /// </summary>
        /// <param name="amount">Rotation amount</param>
        public void VerticalRotation(float amount)
        {
            transform.RotateAround(Target, transform.right, -amount * m_Speed);
        }
        /// <summary>
        /// Zoom towards target
        /// </summary>
        /// <param name="amount">Zoom distance</param>
        public void Zoom(float amount)
        {
            float distance = m_Distance - amount * m_ZoomSpeed;
            if (distance > m_MinDistance && distance < m_MaxDistance)
            {
                transform.position += transform.forward * amount * m_ZoomSpeed;
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