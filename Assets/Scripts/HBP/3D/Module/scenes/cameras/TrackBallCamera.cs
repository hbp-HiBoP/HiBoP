/**
 * \file    TrackBallCamera.cs
 * \author  Lance Florian - Adrien Gannerie
 * \date    2015 - 2017
 * \brief   Define TrackBallCamera class
 */
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace HBP.Module3D.Cam
{
    public enum DisplayedItems{ Meshes, Plots, ROI };

    namespace Events
    {
        /// <summary>
        /// Event when a left click occurs in the camera (params : ray, spScene, idColumn)
        /// </summary>
        public class LeftClick : UnityEvent<Ray, SceneType, int> { }
        /// <summary>
        /// Event when a left mouse movement occurs in the camera (params : ray, mousePosition, spScene, idColumn)
        /// </summary>
        public class MouseMovement : UnityEvent<Ray, Vector3, SceneType, int> { }            
    }

    /// <summary>
    /// The base scene 3D camera class, can move around a target and manage line and column position
    /// </summary>
    [AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
    public abstract class TrackBallCamera : MonoBehaviour
    {
        #region Properties
        protected CameraType m_Type;
        public CameraType Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                m_Type = value;
                int cullingMask = -1;
                switch (m_Type)
                {
                    case CameraType.EEG:
                        cullingMask = EEGCullingMask;
                        break;
                    case CameraType.fMRI:
                        cullingMask = FMRICullingMask;
                        break;
                }
                GetComponent<Camera>().cullingMask = cullingMask;
            }
        }

        [SerializeField,Candlelight.PropertyBackingField]
        protected bool m_IsMinimized = false;      /**< is the camera minimized */
        public bool IsMinimized
        {
            get { return m_IsMinimized; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected int m_EEGCullingMask;
        public int EEGCullingMask
        {
            get { return m_EEGCullingMask; }
            set { m_EEGCullingMask = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected int m_MinimizedCullingMask;
        public int MinimizedCullingMask
        {
            get { return m_MinimizedCullingMask; }
            set { m_MinimizedCullingMask = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected int m_FMRICullingMask;
        public int FMRICullingMask
        {
            get { return m_FMRICullingMask; }
            set { m_FMRICullingMask = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected string m_ColumnLayer;
        public string ColumnLayer
        {
            get { return m_ColumnLayer; }
            set { m_ColumnLayer = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected float m_StartDistance = 250.0f;
        public float StartDistance
        {
            get { return m_StartDistance; }
            set { m_StartDistance = value; }
        } /**< start distance from the target */

        [SerializeField, Candlelight.PropertyBackingField]
        protected float m_Speed = 50.0f;
        public float Speed
        {
            get { return m_Speed; }
            set { m_Speed = value; }
        } /**< camera speed rotation and strafe  */

        [SerializeField, Candlelight.PropertyBackingField]
        protected float m_ZoomSpeed = 3.5f; 
        public float ZoomSpeed
        {
            get { return m_ZoomSpeed; }
            set { m_ZoomSpeed = value; }
        } /**< camera speed zoom */

        [SerializeField, Candlelight.PropertyBackingField]
        protected float m_MinDistance = 50.0f;
        public float MinDistance
        {
            get { return m_MinDistance; }
            set { m_MinDistance = value; }
        } /**< minimal distance from the target */

        [SerializeField, Candlelight.PropertyBackingField]
        protected float m_MaxDistance = 750.0f;
        public float MaxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; }
        } /**< maximal distance from the target */

        [SerializeField, Candlelight.PropertyBackingField]
        protected Color m_SelectedColor;
        public Color SelectedColor
        {
            get { return m_SelectedColor; }
            set { m_SelectedColor = value; }
        } /**< color of the backgound when the camera column is selected */

        [SerializeField, Candlelight.PropertyBackingField]
        protected Color m_UnselectedColor;
        public Color UnselectedColor 
        {
            get { return m_UnselectedColor; }
            set { m_UnselectedColor = value; }
        } /**< normal color of the background */

        [SerializeField, Candlelight.PropertyBackingField]
        protected Material m_XCircleMaterial;
        public Material XCircleMaterial
        {
            get { return m_XCircleMaterial; }
            set { m_XCircleMaterial = value; }
        } /**< matierial used for drawing camera x rotation circles */

        [SerializeField, Candlelight.PropertyBackingField]
        protected Material m_YCircleMaterial;
        public Material YCircleMaterial
        {
            get { return m_YCircleMaterial; }
            set { m_YCircleMaterial = value; }
        } /**< matierial used for drawing camera y rotation circles */

        [SerializeField, Candlelight.PropertyBackingField]
        protected Material m_ZCircleMaterial;
        public Material ZCircleMaterial
        {
            get { return m_ZCircleMaterial; }
            set { m_ZCircleMaterial = value; }
        } /**< matierial used for drawing camera z rotation circles */

        protected InputsSceneManager m_InputsSceneManager = null;  /**< inputs scene manager */
        protected Base3DScene m_AssociatedScene = null;            /**< associated 3D scene */       

        protected SceneType m_SceneType;                  /**< is the camera a single patient one ? */
        protected bool m_IsFocusedOn3DModule = true;       /**< is the focus on the 3D module ? */
        public bool IsFocusedOn3DModule
        {
            set
            {
                m_IsFocusedOn3DModule = value;
            }
        }
        protected bool m_IsFocusedOnCamera;               /**< is the focus on the camera ? */
        protected bool m_CameraIsRotating = false;  /**< is the camera rotating ? */
        protected bool m_DisplayRotationCircles = false;   /**< display rotations circles ?*/

        protected int m_Line = 0;           /**< id camera line */
        public int Line
        {
            set
            {
                m_Line = value;
            }
        }
        protected int m_Column = 0;            /**< id camera column */
        public int Column
        {
            set
            {
                m_Column = value;
            }
        }

        protected float m_RotationCirclesRay = 300f;/**< rotations circles ray */

        protected Vector3 m_Target;                 /**< current target of the camera */
        public Vector3 Target
        {
            get
            {
                return m_Target;
            }
        }
        protected Vector3 m_OriginalTarget;         /**< initial target of the camera */
        protected Vector3 m_OriginalRotationEuler;       /**< initial rotation of the camera */

        protected Vector3[] m_XRotationCircleVertices = null; /**< vertices of x rotation circle */
        protected Vector3[] m_YRotationCircleVertices = null; /**< vertices of y rotation circle */
        protected Vector3[] m_ZRotationCircleVertices = null; /**< vertices of z rotation circle */

        protected List<Vector3[]> m_PlanesCutsCirclesVertices = new List<Vector3[]>(); /**< circles for drawing planes cuts in postrender */

        // post render
        public Material m_PlaneMaterial = null;    /**< material used for drawing the planes cuts*/
        protected bool m_DisplayCutsCircles = false;
        public double m_DisplayPlanesTimeRemaining;
        protected double m_DisplayPlanesTimeStart = 0;
        protected double m_DisplayPlanesTimer = 0;
        #endregion

        #region Private Methods
        protected void Awake()
        {
            m_OriginalRotationEuler = transform.localEulerAngles;

            m_InputsSceneManager = StaticComponents.InputsSceneManager;

            m_StartDistance = Mathf.Clamp(m_StartDistance, m_MinDistance, m_MaxDistance);

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
        protected void OnPreCull()
        {
            m_AssociatedScene.ResetRenderingSettings(GetComponent<Transform>().eulerAngles);
        }
        protected void OnPreRender()
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-OnPreRender");

            if (m_Line == 0)
            {
                if(!m_IsMinimized)
                    m_AssociatedScene.UpdateColumnRendering(m_Column);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        protected void OnPostRender()
        {
            DrawGL();
            m_DisplayRotationCircles = false;
        }
        protected void Update()
        {
            // update current color
            int id = m_AssociatedScene.RetrieveCurrentSelectedColumnID();
            if (id == m_Column)
            {
                GetComponent<Camera>().backgroundColor = m_SelectedColor;
            }
            else
            {
                GetComponent<Camera>().backgroundColor = m_UnselectedColor;
            }

            if (!m_IsMinimized && m_IsFocusedOnCamera && m_IsFocusedOn3DModule)                
                SendMouseEvents();
          
            StartCoroutine("drawGL");
        }
        protected void OnGUI()
        {
            m_IsFocusedOnCamera = IsFocused();

            if (m_IsMinimized || !m_IsFocusedOnCamera || !m_IsFocusedOn3DModule)
                return;

            Event currEvent = Event.current;
            if (Input.anyKey)
            {
                if (Input.GetKey(KeyCode.R))
                    ResetTarget();

                // check keybord zooms
                if (Input.GetKey(KeyCode.A))
                    MoveForward(m_ZoomSpeed);                    

                if (Input.GetKey(KeyCode.E))
                    MoveBackward(m_ZoomSpeed);

                if (Input.GetKey(KeyCode.Z))
                    VerticalRotation(true, 0.2f);

                if (Input.GetKey(KeyCode.S))
                    VerticalRotation(false, 0.2f);

                if (Input.GetKey(KeyCode.Q))
                    HorizontalRotation(true, 0.2f);

                if (Input.GetKey(KeyCode.D))
                    HorizontalRotation(false, 0.2f);

                if (Input.GetKey(KeyCode.LeftArrow))
                    HorizontalStrafe(true, -0.5f);

                if (Input.GetKey(KeyCode.RightArrow))
                    HorizontalStrafe(false, -0.5f);

                if (Input.GetKey(KeyCode.UpArrow))
                    VerticalStrafe(true, -0.5f);

                if (Input.GetKey(KeyCode.DownArrow))
                    VerticalStrafe(false, -0.5f);

                if (currEvent.type == EventType.KeyDown)
                {
                    m_InputsSceneManager.send_keyboard_action_to_scenes(m_AssociatedScene, currEvent.keyCode);
                }                

                if (Input.GetKey(KeyCode.Space))
                    m_AssociatedScene.DisplaySitesName(GetComponent<Camera>());
            }
            else if(currEvent.type == EventType.ScrollWheel)
            {
                m_InputsSceneManager.send_scroll_mouse_to_scenes(m_AssociatedScene, Input.mouseScrollDelta);
            }
        }
        /// <summary>
        /// Check and send the mouse events to the mouse manager and apply cameras rotations and straffes
        /// </summary>
        protected void SendMouseEvents()
        {
            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

            // mouse movement
            m_InputsSceneManager.send_mouse_movement_to_scenes(ray, m_AssociatedScene, Input.mousePosition, m_Column);

            // left click
            if (Input.GetMouseButtonUp(0))
            {
                m_InputsSceneManager.send_click_ray_to_scenes(ray, m_AssociatedScene, m_Column);
            }


            // right click
            if (Input.GetMouseButton(1))
            {
                float nx = 0;
                float ny = 0;
                nx = Input.GetAxis("Mouse X");
                ny = Input.GetAxis("Mouse Y");

                // check horizontal right click mouse drag movement
                if (nx != 0)
                    if (nx < 0)
                        HorizontalRotation(true, -nx * m_Speed);
                    else
                        HorizontalRotation(false, nx * m_Speed);

                // check vertical right click mouse drag movement
                if (ny != 0)
                    if (ny < 0)
                        VerticalRotation(true, ny * m_Speed);
                    else
                        VerticalRotation(false, -ny * m_Speed);
            }

            if (Input.GetMouseButton(2))
            {
                float nx = 0;
                float ny = 0;
                nx = Input.GetAxis("Mouse X");
                ny = Input.GetAxis("Mouse Y");

                // check horizontal right click mouse drag movement
                if (nx != 0)
                    if (nx < 0)
                        HorizontalStrafe(true, nx * m_Speed);
                    else
                        HorizontalStrafe(false, -nx * m_Speed);


                // check vertical right click mouse drag movement
                if (ny != 0)
                    if (ny < 0)
                        VerticalStrafe(true, -ny * m_Speed);
                    else
                        VerticalStrafe(false, ny * m_Speed);
            }
        }
        /// <summary>
        /// Strafe hozizontally the camera position and target with the same vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        protected void HorizontalStrafe(bool left, float amount)
        {
            m_DisplayRotationCircles = true;
            Vector3 strafe;
            if (left)
                strafe = -transform.right * amount;
            else
                strafe = transform.right * amount;

            transform.position = transform.position + strafe;
            m_Target = m_Target + strafe;
        }
        /// <summary>
        /// Strafe vertically the camera position and target with the same vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        protected void VerticalStrafe(bool up, float amount)
        {
            m_DisplayRotationCircles = true;
            Vector3 strafe;
            if (up)
                strafe = transform.up * amount;
            else
                strafe = -transform.up * amount;

            transform.position = transform.position + strafe;
            m_Target = m_Target + strafe;
        }
        /// <summary>
        /// Turn horizontally around the camera target
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        protected void HorizontalRotation(bool left, float amount)
        {
            m_DisplayRotationCircles = true;
            Vector3 vecTargetPos_EyePos = transform.position - m_Target;
            Quaternion rotation;
            if (left)
                rotation = Quaternion.AngleAxis(-amount, transform.up);
            else
                rotation = Quaternion.AngleAxis(amount, transform.up);

            transform.position = rotation * vecTargetPos_EyePos + m_Target;
            transform.LookAt(m_Target, transform.up);
        }
        /// <summary>
        /// Turn vertically around the camera target
        /// </summary>
        /// <param name="up"></param>
        /// <param name="amount"></param>
        protected void VerticalRotation(bool up, float amount)
        {
            m_DisplayRotationCircles = true;
            Vector3 vecTargetPos_EyePos = transform.position - m_Target;
            Quaternion rotation;
            if (up)
                rotation = Quaternion.AngleAxis(-amount, transform.right);
            else
                rotation = Quaternion.AngleAxis(amount, transform.right);

            transform.position = rotation * vecTargetPos_EyePos + m_Target;
            transform.LookAt(m_Target, Vector3.Cross(m_Target - transform.position, transform.right));
        }
        /// <summary>
        /// Move forward the position in the direction of the target
        /// </summary>
        /// <param name="amount"></param>
        protected void MoveForward(float amount)
        {
            float length = Vector3.Distance(transform.position, m_Target);
            if (length - amount > m_MinDistance)
            {
                transform.position += transform.forward * amount;
            }
        }
        /// <summary>
        /// Move backward  the position in the direction of the target
        /// </summary>
        /// <param name="amount"></param>
        protected void MoveBackward(float amount)
        {
            float length = Vector3.Distance(transform.position, m_Target);
            if (length + amount < m_MaxDistance)
            {
                transform.position -= transform.forward * amount;
            }
        }
        /// <summary>
        /// Reset the original target of the camera
        /// </summary>
        protected void ResetTarget()
        {
            transform.localEulerAngles = m_OriginalRotationEuler;
            m_Target = m_OriginalTarget;
            transform.position = m_Target - transform.forward * m_StartDistance;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public void DrawGL()
        {
            if (!m_IsFocusedOnCamera || m_IsMinimized)
                return;

            if(m_DisplayCutsCircles)
            {
                m_DisplayPlanesTimer = TimeExecution.get_world_time() - m_DisplayPlanesTimeStart;
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
                        GL.Vertex(m_PlanesCutsCirclesVertices[ii][3 *m_PlanesCutsCirclesVertices[ii].Length / 8]);
                        GL.Vertex(m_PlanesCutsCirclesVertices[ii][7 * m_PlanesCutsCirclesVertices[ii].Length / 8]);
                        GL.End();
                    }
                }
                else
                    m_DisplayCutsCircles = false;
            }

            if (m_DisplayRotationCircles)
            {
                //GL.PushMatrix();
                m_XCircleMaterial.SetPass(0);

                float currentDist = Vector3.Distance(transform.position, m_Target);
                float scaleRatio = currentDist / m_MaxDistance;

                for (int ii = 0; ii < m_XRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(m_Target + scaleRatio * m_XRotationCircleVertices[ii]);
                    GL.Vertex(m_Target + scaleRatio * m_XRotationCircleVertices[(ii + 1) % m_XRotationCircleVertices.Length]);
                    GL.End();
                }

                m_YCircleMaterial.SetPass(0);

                for (int ii = 0; ii < m_YRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(m_Target + scaleRatio * m_YRotationCircleVertices[ii]);
                    GL.Vertex(m_Target + scaleRatio * m_YRotationCircleVertices[(ii + 1) % m_YRotationCircleVertices.Length]);
                    GL.End();
                }

                m_ZCircleMaterial.SetPass(0);

                for (int ii = 0; ii < m_ZRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(m_Target + scaleRatio * m_ZRotationCircleVertices[ii]);
                    GL.Vertex(m_Target + scaleRatio * m_ZRotationCircleVertices[(ii + 1) % m_ZRotationCircleVertices.Length]);
                    GL.End();
                }                
            }
        }
        /// <summary>
        ///  init the camera
        /// </summary>
        /// <param name="position"></param>
        public void Initialize(Vector3 position)
        {
            transform.localEulerAngles = m_OriginalRotationEuler;
            m_Target = position;
            m_OriginalTarget = m_Target;            
            transform.position = m_Target - transform.forward * m_StartDistance;
        }
        /// <summary>
        /// stop the rotation of the camera
        /// </summary>
        public void StopAutomaticRotation()
        {            
            m_CameraIsRotating = false;
        }
        /// <summary>
        /// state the rotation of the camera
        /// </summary>
        public void AutomaticCameraRotation()
        {
            StartCoroutine("rotate_360");
        }
        /// <summary>
        /// Corountine for rotating the camera
        /// </summary>
        /// <returns></returns>
        private IEnumerator Rotate360() //DELETEME
        {
            float timeFunction = 5f;

            m_CameraIsRotating = true;
            while (m_CameraIsRotating)
            {
                
                float startTime = Time.time;
                float totalRotation = 0f;
                float currentRotationState;
                float rotationToDo;

                bool end = false;
                while (!end)
                {
                    if (!m_CameraIsRotating)
                        break;                        

                    // retrieve elapsed time
                    float elapsedTime = (Time.time - startTime);

                    // check if finished
                    if (elapsedTime >= timeFunction)
                        end = true;
                    else
                    {
                        // compute current rotation to do
                        currentRotationState = (elapsedTime / timeFunction) * 360f;
                        rotationToDo = currentRotationState - totalRotation;

                        // do the rotation
                        HorizontalRotation(false, rotationToDo);

                        // update total rotation
                        totalRotation += rotationToDo;
                    }

                    yield return null;
                }
            }
        }
        /// <summary>
        /// Check if the camera is in the current selected column
        /// </summary>
        /// <returns></returns>
        public bool IsSelected()
        {
            return (m_AssociatedScene.RetrieveCurrentSelectedColumnID() == m_Column);
        }
        /// <summary>
        /// Update the culling mask rendered of the camera
        /// </summary>
        /// <param name="cullingMask"></param>
        /// <param name="fMRI"></param>
        public void UpdateCullingMask(int cullingMask, bool fMRI = false)
        {
            if (fMRI)
            {
                FMRICullingMask = cullingMask;

                if (!m_IsMinimized)
                {
                    GetComponent<Camera>().cullingMask = FMRICullingMask;
                }
            }
            else
            {
                EEGCullingMask = cullingMask;

                if (!m_IsMinimized && Type == CameraType.EEG)
                {
                    GetComponent<Camera>().cullingMask = EEGCullingMask;
                }
            }                
        }
        /// <summary>
        /// Set the minimized state of the camera
        /// </summary>
        /// <param name="state"></param>
        public void SetMinimizedState(bool state)
        {
            m_IsMinimized = state;
            int cullingMask = -1;
            if(m_IsMinimized)
            {
                cullingMask = MinimizedCullingMask;
            }
            else
            {
                switch(Type)
                {
                    case CameraType.EEG: cullingMask = EEGCullingMask;
                        break;
                    case CameraType.fMRI: cullingMask = FMRICullingMask;
                        break;
                }
            }
            GetComponent<Camera>().cullingMask = cullingMask;
        }
        /// <summary>
        /// Check if the mouse is inside the camera rectangle
        /// </summary>
        /// <returns></returns>
        public bool IsFocused()
        {
            return (GetComponent<Camera>().pixelRect.Contains(Input.mousePosition));
        }
        /// <summary>
        /// Update the selected column with the associated scene
        /// </summary>
        /// <param name="idColumn"></param>
        public void UpdateSelectedColumn(int idColumn)
        {
            m_AssociatedScene.UpdateSelectedColumn(idColumn);
        }
        /// <summary>
        /// Define the camera with a position a rotation and it's target.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="target"></param>
        public void DefineCamera(Vector3 position, Quaternion rotation, Vector3 target)
        {
            transform.position = position;
            transform.rotation = rotation;
            this.m_Target = target;
        }
        #endregion
    }
}