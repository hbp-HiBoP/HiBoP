
/**
 * \file    TrackBallCamera.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TrackBallCamera class
 */

// system
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.Events;


namespace HBP.VISU3D.Cam
{
    public enum DisplayedItems : int
    {
        Meshes, Plots, ROI
    };

    namespace Events
    {
        /// <summary>
        /// Event when a left click occurs in the camera (params : ray, spScene, idColumn)
        /// </summary>
        public class LeftClick : UnityEvent<Ray, bool, int> { }

        /// <summary>
        /// Event when a left mouse movement occurs in the camera (params : ray, mousePosition, spScene, idColumn)
        /// </summary>
        public class MouseMovement : UnityEvent<Ray, Vector3, bool, int> { }            
    }

    /// <summary>
    /// The base scene 3D camera class, can move around a target and manage line and column position
    /// </summary>
    [AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
    public abstract class TrackBallCamera : MonoBehaviour
    {
        #region members
        
        public bool m_fMRI = false;             /**< is an IRMF camera ? */
        public bool m_isMinimized = false;      /**< is the camera minimized */

        public int m_cullingMask;               /**< culling mask of the camera */
        public int m_minimizedCullingMask;      /**< culling mask of the camera in minimized state */
        public int m_IRMFCullingMask;           /**< culling mask of the camera if IRMF */

        public string m_columnLayer;            /**< layer of the column */

        public float m_startDistance = 250.0f;  /**< start distance from the target */
        public float m_speed = 50.0f;           /**< camera speed rotation and strafe  */
        public float m_zoomSpeed = 3.5f;        /**< camera speed zoom */
        public float m_minDistance = 50.0f;     /**< minimal distance from the target */
        public float m_maxDistance = 750.0f;    /**< maximal distance from the target */

        public Color m_selectedColumnColor;     /**< color of the backgound when the camera column is selected */
        public Color m_normalColor;             /**< normal color of the background */

        public Material m_xCircleMat = null;    /**< matierial used for drawing camera x rotation circles */
        public Material m_yCircleMat = null;    /**< matierial used for drawing camera y rotation circles */
        public Material m_zCircleMat = null;    /**< matierial used for drawing camera z rotation circles */

        public InputsSceneManager m_inputsSceneManager = null;  /**< inputs scene manager */
        public Base3DScene m_associatedScene = null;            /**< associated 3D scene */

        
        protected bool m_spCamera;                  /**< is the camera a single patient one ? */
        protected bool m_moduleFocus = true;       /**< is the focus on the 3D module ? */
        protected bool m_cameraFocus;               /**< is the focus on the camera ? */
        protected bool m_cameraIsRotating = false;  /**< is the camera rotating ? */
        protected bool m_displayRotationCircles = false;   /**< display rotations circles ?*/

        protected int m_idLineCamera = 0;           /**< id camera line */
        protected int m_idColCamera = 0;            /**< id camera column */

        protected float m_rotationCirclesRay = 300f;/**< rotations circles ray */
        protected float m_cameraRotationSpeed = 50.0f; /**< rotations speed */

        protected Vector3 m_target;                 /**< current target of the camera */
        protected Vector3 m_originalTarget;         /**< initial target of the camera */
        protected Vector3 m_originalRotationEuler;       /**< initial rotation of the camera */

        protected Vector3[] m_xRotationCircleVertices = null; /**< vertices of x rotation circle */
        protected Vector3[] m_yRotationCircleVertices = null; /**< vertices of y rotation circle */
        protected Vector3[] m_zRotationCircleVertices = null; /**< vertices of z rotation circle */

        protected List<Vector3[]> m_planesCutsCirclesVertices = new List<Vector3[]>(); /**< circles for drawing planes cuts in postrender */

        // post render
        public Material m_planeMat = null;    /**< material used for drawing the planes cuts*/
        protected bool m_displayCutsCircles = false;
        public double m_displayPlanesTimeRemaining;
        protected double m_displayPlanesTimeStart = 0;
        protected double m_displayPlanesTimer = 0;

        #endregion members

        #region monoBehaviour

        protected void Awake()
        {
            m_originalRotationEuler = transform.localEulerAngles;

            m_inputsSceneManager = StaticComponents.MouseSceneManager;

            // check parameters integrity
            if (m_startDistance < m_minDistance)
                m_startDistance = m_minDistance;

            if (m_startDistance > m_maxDistance)
                m_startDistance = m_maxDistance;

            // rotation circles
            m_xRotationCircleVertices = Geometry.create_3D_circle_points(new Vector3(0, 0, 0), m_rotationCirclesRay, 150);
            m_yRotationCircleVertices = Geometry.create_3D_circle_points(new Vector3(0, 0, 0), m_rotationCirclesRay, 150);
            m_zRotationCircleVertices = Geometry.create_3D_circle_points(new Vector3(0, 0, 0), m_rotationCirclesRay, 150);

            for (int ii = 0; ii < m_xRotationCircleVertices.Length; ++ii)
            {
                m_xRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.up) * m_xRotationCircleVertices[ii];
                m_yRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.left) * m_yRotationCircleVertices[ii];
            }
        }

        protected void OnPreCull()
        {
            m_associatedScene.reset_rendering_settings(GetComponent<Transform>().eulerAngles);
        }

        protected void OnPreRender()
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-OnPreRender");

            if (m_idLineCamera == 0)
            {
                if(!m_isMinimized)
                    m_associatedScene.update_column_rendering(m_idColCamera);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }


        protected void OnPostRender()
        {
            drawGL();
            m_displayRotationCircles = false;
        }

        protected void Update()
        {
            // update current color
            int id = m_associatedScene.retrieve_current_selected_column_id();
            if (id == m_idColCamera)
            {
                GetComponent<Camera>().backgroundColor = m_selectedColumnColor;
            }
            else
            {
                GetComponent<Camera>().backgroundColor = m_normalColor;
            }

            if (!m_isMinimized && m_cameraFocus && m_moduleFocus)                
                send_mouse_events();
          
            StartCoroutine("drawGL");
            automatic_camera_rotation();
        }


        /// <summary>
        /// Called multiple times per frame in response to GUI events. The Layout and Repaint events are processed first, followed by a Layout and keyboard/mouse event for each input event.
        /// </summary>
        protected void OnGUI()
        {
            m_cameraFocus = is_focus();

            if (m_isMinimized || !m_cameraFocus || !m_moduleFocus)
                return;

            Event currEvent = Event.current;
            if (Input.anyKey)
            {
                if (Input.GetKey(KeyCode.R))
                    reset_target();

                // check keybord zooms
                if (Input.GetKey(KeyCode.A))
                    move_forward(m_zoomSpeed);                    

                if (Input.GetKey(KeyCode.E))
                    move_backward(m_zoomSpeed);

                if (Input.GetKey(KeyCode.Z))
                    vertical_rotation(true, 0.2f);

                if (Input.GetKey(KeyCode.S))
                    vertical_rotation(false, 0.2f);

                if (Input.GetKey(KeyCode.Q))
                    horizontal_rotation(true, 0.2f);

                if (Input.GetKey(KeyCode.D))
                    horizontal_rotation(false, 0.2f);

                if (Input.GetKey(KeyCode.LeftArrow))
                    horizontal_strafe(true, -0.5f);

                if (Input.GetKey(KeyCode.RightArrow))
                    horizontal_strafe(false, -0.5f);

                if (Input.GetKey(KeyCode.UpArrow))
                    vertical_strafe(true, -0.5f);

                if (Input.GetKey(KeyCode.DownArrow))
                    vertical_strafe(false, -0.5f);

                if (currEvent.type == EventType.KeyDown)
                {
                    m_inputsSceneManager.send_keyboard_action_to_scenes(m_spCamera, currEvent.keyCode);
                }                

                if (Input.GetKey(KeyCode.Space))
                    m_associatedScene.display_sites_names(GetComponent<Camera>());
            }
            else if(currEvent.type == EventType.ScrollWheel)
            {
                m_inputsSceneManager.send_scroll_mouse_to_scenes(m_spCamera, Input.mouseScrollDelta);
            }
        }

        #endregion monoBehaviour

        #region others

        public void drawGL()
        {
            if (!m_cameraFocus || m_isMinimized)
                return;

            if(m_displayCutsCircles)
            {
                m_displayPlanesTimer = TimeExecution.get_world_time() - m_displayPlanesTimeStart;
                if (m_displayPlanesTimeRemaining > m_displayPlanesTimer)
                {
                    m_planeMat.SetPass(0);
                    
                    {
                        int ii = m_associatedScene.data_.lastIdPlaneModified;
                        for (int jj = 0; jj < m_planesCutsCirclesVertices[ii].Length; ++jj)
                        {
                            GL.Begin(GL.LINES);
                            GL.Vertex(m_planesCutsCirclesVertices[ii][jj]);
                            GL.Vertex(m_planesCutsCirclesVertices[ii][(jj + 1) % m_planesCutsCirclesVertices[ii].Length]);
                            GL.End();
                        }

                        GL.Begin(GL.LINES);
                        GL.Vertex(m_planesCutsCirclesVertices[ii][m_planesCutsCirclesVertices[ii].Length / 8]);
                        GL.Vertex(m_planesCutsCirclesVertices[ii][5 * m_planesCutsCirclesVertices[ii].Length / 8]);
                        GL.End();
                        GL.Begin(GL.LINES);
                        GL.Vertex(m_planesCutsCirclesVertices[ii][3 *m_planesCutsCirclesVertices[ii].Length / 8]);
                        GL.Vertex(m_planesCutsCirclesVertices[ii][7 * m_planesCutsCirclesVertices[ii].Length / 8]);
                        GL.End();
                    }
                }
                else
                    m_displayCutsCircles = false;
            }

            if (m_displayRotationCircles)
            {
                //GL.PushMatrix();
                m_xCircleMat.SetPass(0);

                float currentDist = Vector3.Distance(transform.position, m_target);
                float scaleRatio = currentDist / m_maxDistance;

                for (int ii = 0; ii < m_xRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(m_target + scaleRatio * m_xRotationCircleVertices[ii]);
                    GL.Vertex(m_target + scaleRatio * m_xRotationCircleVertices[(ii + 1) % m_xRotationCircleVertices.Length]);
                    GL.End();
                }

                m_yCircleMat.SetPass(0);

                for (int ii = 0; ii < m_yRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(m_target + scaleRatio * m_yRotationCircleVertices[ii]);
                    GL.Vertex(m_target + scaleRatio * m_yRotationCircleVertices[(ii + 1) % m_yRotationCircleVertices.Length]);
                    GL.End();
                }

                m_zCircleMat.SetPass(0);

                for (int ii = 0; ii < m_zRotationCircleVertices.Length; ++ii)
                {
                    GL.Begin(GL.LINES);
                    GL.Vertex(m_target + scaleRatio * m_zRotationCircleVertices[ii]);
                    GL.Vertex(m_target + scaleRatio * m_zRotationCircleVertices[(ii + 1) % m_zRotationCircleVertices.Length]);
                    GL.End();
                }                
            }
        }

        /// <summary>
        ///  init the camera
        /// </summary>
        /// <param name="position"></param>
        public void init(Vector3 position)
        {
            transform.localEulerAngles = m_originalRotationEuler;
            m_target = position;
            m_originalTarget = m_target;            
            transform.position = m_target - transform.forward * m_startDistance;
        }

        /// <summary>
        /// stop the rotation of the camera
        /// </summary>
        public void stop_automatic_rotation()
        {            
            m_cameraIsRotating = false;
        }

        /// <summary>
        /// starts the rotation of the camera
        /// </summary>
        public void start_automatic_rotation()
        {
            m_cameraIsRotating = true;
        }

        /// <summary>
        /// makes the camera rotate
        /// </summary>
        public void automatic_camera_rotation()
        {
            if (m_cameraIsRotating)
            {
                horizontal_rotation(false, m_cameraRotationSpeed*Time.deltaTime);
            }
        }

        /// <summary>
        /// sets the speed of the camera rotation
        /// </summary>
        public void set_camera_rotation_speed(float speed)
        {
            m_cameraRotationSpeed = speed;
        }

        /// <summary>
        /// Corountine for rotating the camera
        /// </summary>
        /// <returns></returns>
        private IEnumerator rotate_360()
        {
            float timeFunction = 5f;

            m_cameraIsRotating = true;
            while (m_cameraIsRotating)
            {
                
                float startTime = Time.time;
                float totalRotation = 0f;
                float currentRotationState;
                float rotationToDo;

                bool end = false;
                while (!end)
                {
                    if (!m_cameraIsRotating)
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
                        horizontal_rotation(false, rotationToDo);

                        // update total rotation
                        totalRotation += rotationToDo;
                    }

                    yield return null;
                }
            }
        }


        /// <summary>
        /// Set the focus state of the module
        /// </summary>
        /// <param name="state"></param>
        public void set_camera_focus(bool state)
        {
            m_moduleFocus = state;
        }

        /// <summary>
        /// Update the culling of the camera for fMRI
        /// </summary>
        /// <param name="spScene"></param>
        public void set_fMRI_camera(bool isFmri)
        {            
            m_fMRI = isFmri;
            if(m_fMRI)
                GetComponent<Camera>().cullingMask = m_IRMFCullingMask;
            else
                GetComponent<Camera>().cullingMask = m_cullingMask;
        }

        /// <summary>
        /// Define the line id of the camera
        /// </summary>
        /// <param name="newLineId"></param>
        public void set_line_ID(int newLineId) { m_idLineCamera = newLineId; }

        /// <summary>
        /// Define the column id of the camera
        /// </summary>
        /// <param name="newColId"></param>
        public void set_column_ID(int newColId){ m_idColCamera = newColId; }

        /// <summary>
        /// Define the column layer
        /// </summary>
        /// <param name="columnLayer"></param>
        public void set_column_layer(string columnLayer) { m_columnLayer = columnLayer; }

        /// <summary>
        /// Check if the camera is minimized
        /// </summary>
        /// <returns></returns>
        public bool is_minimized() { return m_isMinimized; }

        /// <summary>
        /// Check if the camera is in the current selected column
        /// </summary>
        /// <returns></returns>
        public bool is_selected()
        {
            return (m_associatedScene.retrieve_current_selected_column_id() == m_idColCamera);
        }

        /// <summary>
        /// Update the culling mask rendered of the camera
        /// </summary>
        /// <param name="cullingMask"></param>
        /// <param name="fMRI"></param>
        public void update_culling_mask(int cullingMask, bool fMRI = false)
        {
            if (fMRI)
            {
                m_IRMFCullingMask = cullingMask;

                if (!m_isMinimized)
                {
                    GetComponent<Camera>().cullingMask = m_IRMFCullingMask;
                }
            }
            else
            {
                m_cullingMask = cullingMask;

                if (!m_isMinimized && !m_fMRI)
                {
                    GetComponent<Camera>().cullingMask = m_cullingMask;
                }
            }                
        }


        /// <summary>
        /// Set the minimized state of the camera
        /// </summary>
        /// <param name="state"></param>
        public void set_minimized_state(bool state)
        {
            m_isMinimized = state;
            GetComponent<Camera>().cullingMask = m_isMinimized ? m_minimizedCullingMask : (m_fMRI ? m_IRMFCullingMask : m_cullingMask);
        }

        /// <summary>
        /// Check if the mouse is inside the camera rectangle
        /// </summary>
        /// <returns></returns>
        public bool is_focus()
        {
            return (GetComponent<Camera>().pixelRect.Contains(Input.mousePosition));
        }

        /// <summary>
        /// Check and send the mouse events to the mouse manager and apply cameras rotations and straffes
        /// </summary>
        protected void send_mouse_events()
        {
            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

            // mouse movement
            m_inputsSceneManager.send_mouse_movement_to_scenes(ray, m_spCamera, Input.mousePosition, m_idColCamera);

            // left click
            if (Input.GetMouseButtonUp(0))
            {
                m_inputsSceneManager.send_click_ray_to_scenes(ray, m_spCamera, m_idColCamera);
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
                        horizontal_rotation(true, -nx * m_speed);
                    else 
                        horizontal_rotation(false, nx * m_speed);
                
                // check vertical right click mouse drag movement
                if (ny != 0)
                    if (ny < 0)
                        vertical_rotation(true,  ny * m_speed);
                    else
                        vertical_rotation(false,-ny * m_speed);
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
                        horizontal_strafe(true,  nx * m_speed);
                    else
                        horizontal_strafe(false,-nx * m_speed);


                // check vertical right click mouse drag movement
                if (ny != 0)
                    if (ny < 0)
                        vertical_strafe(true, -ny * m_speed);
                    else
                        vertical_strafe(false, ny * m_speed);
            }
        }

        /// <summary>
        /// Update the selected column with the associated scene
        /// </summary>
        /// <param name="idColumn"></param>
        public void update_selected_column(int idColumn)
        {
            m_associatedScene.update_selected_column(idColumn);
        }


        /// <summary>
        /// Strafe hozizontally the camera position and target with the same vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        protected void horizontal_strafe(bool left, float amount)
        {
            m_displayRotationCircles = true;
            Vector3 strafe;
            if(left)
                strafe = -transform.right * amount;
            else
                strafe = transform.right * amount;

            transform.position = transform.position + strafe;
            m_target = m_target + strafe;
        }

        /// <summary>
        /// Strafe vertically the camera position and target with the same vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        protected void vertical_strafe(bool up, float amount)
        {
            m_displayRotationCircles = true;
            Vector3 strafe;
            if (up)
                strafe = transform.up * amount;
            else
                strafe = -transform.up * amount;

            transform.position = transform.position + strafe;
            m_target = m_target + strafe;
        }

        /// <summary>
        /// Turn horizontally around the camera target
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        protected void horizontal_rotation(bool left, float amount)
        {
            m_displayRotationCircles = true;
            Vector3 vecTargetPos_EyePos = transform.position - m_target;
            Quaternion rotation;
            if(left)
                rotation = Quaternion.AngleAxis(-amount, transform.up);
            else
                rotation = Quaternion.AngleAxis(amount, transform.up);

            transform.position = rotation * vecTargetPos_EyePos + m_target;
            transform.LookAt(m_target, transform.up);
        }

        /// <summary>
        /// Turn vertically around the camera target
        /// </summary>
        /// <param name="up"></param>
        /// <param name="amount"></param>
        protected void vertical_rotation(bool up, float amount)
        {
            m_displayRotationCircles = true;
            Vector3 vecTargetPos_EyePos = transform.position - m_target;
            Quaternion rotation;
            if (up)
                rotation = Quaternion.AngleAxis(-amount, transform.right); 
            else
                rotation = Quaternion.AngleAxis(amount, transform.right); 

            transform.position = rotation * vecTargetPos_EyePos + m_target;
            transform.LookAt(m_target, Vector3.Cross(m_target - transform.position, transform.right));            
        }


        /// <summary>
        /// Move forward the position in the direction of the target
        /// </summary>
        /// <param name="amount"></param>
        protected void move_forward(float amount)
        {
            float length = Vector3.Distance(transform.position, m_target);
            if (length - amount > m_minDistance)
            {
                transform.position += transform.forward * amount;
            }
        }

        /// <summary>
        /// Move backward  the position in the direction of the target
        /// </summary>
        /// <param name="amount"></param>
        protected void move_backward(float amount)
        {
            float length = Vector3.Distance(transform.position, m_target);
            if (length + amount < m_maxDistance)
            {
                transform.position -= transform.forward * amount;
            }
        }

        /// <summary>
        /// Reset the original target of the camera
        /// </summary>
        protected void reset_target()
        {
            transform.localEulerAngles = m_originalRotationEuler;
            m_target = m_originalTarget;
            transform.position = m_target - transform.forward * m_startDistance;
        }

        /// <summary>
        /// Define the camera with a position a rotation and it's target.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="target"></param>
        public void define_camera(Vector3 position, Quaternion rotation, Vector3 target)
        {
            transform.position = position;
            transform.rotation = rotation;
            this.m_target = target;
        }

        /// <summary>
        /// Return the target of the camera
        /// </summary>
        /// <returns></returns>
        public Vector3 target()
        {
            return m_target;
        }

        #endregion others
    }
}