
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
        
        public bool m_IRMF = false;             /**< is an IRMF camera ? */
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
        protected bool m_moduleFocus = false;       /**< is the focus on the 3D module ? */
        protected bool m_cameraFocus;               /**< is the focus on the camera ? */
        protected bool m_cameraIsRotating = false;  /**< is the camera rotating ? */
        protected bool m_isMouseClicking = false;   /**< is the mouse clicking ?*/

        protected int m_idLineCamera = 0;           /**< id camera line */
        protected int m_idColCamera = 0;            /**< id camera column */

        protected float m_rotationCirclesRay = 300f;/**< rotations circles ray */

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

            m_inputsSceneManager = StaticVisuComponents.MouseSceneManager;

            // check parameters integrity
            if (m_startDistance < m_minDistance)
                m_startDistance = m_minDistance;

            if (m_startDistance > m_maxDistance)
                m_startDistance = m_maxDistance;

            // rotation circles
            m_xRotationCircleVertices = Geometry.create3DCirclePoints(new Vector3(0, 0, 0), m_rotationCirclesRay, 150);
            m_yRotationCircleVertices = Geometry.create3DCirclePoints(new Vector3(0, 0, 0), m_rotationCirclesRay, 150);
            m_zRotationCircleVertices = Geometry.create3DCirclePoints(new Vector3(0, 0, 0), m_rotationCirclesRay, 150);

            for (int ii = 0; ii < m_xRotationCircleVertices.Length; ++ii)
            {
                m_xRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.up) * m_xRotationCircleVertices[ii];
                m_yRotationCircleVertices[ii] = Quaternion.AngleAxis(90, Vector3.left) * m_yRotationCircleVertices[ii];
            }
        }

        /// <summary>
        /// Called before the camera culls the scene. Culling determines which objects are visible to the camera. OnPreCull is called just before culling takes place.
        /// </summary>
        protected void OnPreCull()
        {
            m_associatedScene.resetRenderingSettings(GetComponent<Transform>().eulerAngles);
        }

        /// <summary>
        /// Called before the camera starts rendering the scene.
        /// </summary>
        protected void OnPreRender()
        {
            Profiler.BeginSample("TEST-OnPreRender");

            if (m_idLineCamera == 0)
            {
                if(!m_isMinimized)
                    m_associatedScene.updateColumnRender(m_idColCamera);
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// 
        /// </summary>
        protected void OnPostRender()
        {
            drawGL();
        }

        /// <summary>
        /// Update is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
        protected void Update()
        {
            // update current color
            int id = m_associatedScene.retrieveCurrentSelectedColumnId();
            if (id == m_idColCamera)
            {
                GetComponent<Camera>().backgroundColor = m_selectedColumnColor;
            }
            else
            {
                GetComponent<Camera>().backgroundColor = m_normalColor;
            }

            if (!m_isMinimized && m_cameraFocus && m_moduleFocus)
                sendMouveEvents();

            StartCoroutine("drawGL");
        }


        /// <summary>
        /// Called multiple times per frame in response to GUI events. The Layout and Repaint events are processed first, followed by a Layout and keyboard/mouse event for each input event.
        /// </summary>
        protected void OnGUI()
        {
            m_cameraFocus = isFocus();

            if (m_isMinimized || !m_cameraFocus || !m_moduleFocus)
                return;

            Event currEvent = Event.current;
            if(Input.anyKey)
            {
                if (Input.GetKey(KeyCode.Space))
                    resetTarget();
      
                // check keybord zooms
                if (Input.GetKey(KeyCode.Z))
                    moveForward(m_zoomSpeed);

                if (Input.GetKey(KeyCode.S))
                    moveBackward(m_zoomSpeed);

                m_inputsSceneManager.sendKeyboardActionToScene(m_spCamera, currEvent.keyCode);
            }
            else if(currEvent.type == EventType.ScrollWheel)
            {
                m_inputsSceneManager.sendScrollMouseToScenes(m_spCamera, Input.mouseScrollDelta);
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
                m_displayPlanesTimer = TimeExecution.getWorldTime() - m_displayPlanesTimeStart;
                if (m_displayPlanesTimeRemaining > m_displayPlanesTimer)
                {
                    m_planeMat.SetPass(0);
                    //for (int ii = 0; ii < m_planesCutsCirclesVertices.Count; ++ii)
                    {
                        int ii = m_associatedScene.data_.lastIdPlaneModified;
                        for (int jj = 0; jj < m_planesCutsCirclesVertices[ii].Length; ++jj)
                        {
                            GL.Begin(GL.LINES);
                            GL.Vertex(m_planesCutsCirclesVertices[ii][jj]);
                            GL.Vertex(m_planesCutsCirclesVertices[ii][(jj + 1) % m_planesCutsCirclesVertices[ii].Length]);
                            GL.End();
                        }

                        //GL.Begin(GL.LINES);
                        //GL.Vertex(m_planesCutsCirclesVertices[ii][0]);
                        //GL.Vertex(m_planesCutsCirclesVertices[ii][m_planesCutsCirclesVertices[ii].Length / 2]);
                        //GL.End();
                        //GL.Begin(GL.LINES);
                        //GL.Vertex(m_planesCutsCirclesVertices[ii][m_planesCutsCirclesVertices[ii].Length / 4]);
                        //GL.Vertex(m_planesCutsCirclesVertices[ii][3 * m_planesCutsCirclesVertices[ii].Length / 4]);
                        //GL.End();
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

            if (m_isMouseClicking)
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
        public void stopAutomaticRotation()
        {            
            m_cameraIsRotating = false;
        }

        /// <summary>
        /// state the rotation of the camera
        /// </summary>
        public void automaticCameraRotation()
        {
            StartCoroutine("rotate360");
        }

        /// <summary>
        /// Corountine for rotating the camera
        /// </summary>
        /// <returns></returns>
        private IEnumerator rotate360()
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
                        horizontalRotation(false, rotationToDo);

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
        public void setCameraFocus(bool state)
        {
            m_moduleFocus = state;
        }

        /// <summary>
        /// Update the culling of the camera for IRMF
        /// </summary>
        /// <param name="spScene"></param>
        public void setIRMFCamera(bool isIRMF)
        {            
            m_IRMF = isIRMF;
            if(m_IRMF)
                GetComponent<Camera>().cullingMask = m_IRMFCullingMask;
            else
                GetComponent<Camera>().cullingMask = m_cullingMask;
        }

        /// <summary>
        /// Define the line id of the camera
        /// </summary>
        /// <param name="newLineId"></param>
        public void setLineId(int newLineId) { m_idLineCamera = newLineId; }

        /// <summary>
        /// Define the column id of the camera
        /// </summary>
        /// <param name="newColId"></param>
        public void setColId(int newColId){ m_idColCamera = newColId; }

        /// <summary>
        /// Define the column layer
        /// </summary>
        /// <param name="columnLayer"></param>
        public void setColumnLayer(string columnLayer) { m_columnLayer = columnLayer; }

        /// <summary>
        /// Check if the camera is minimized
        /// </summary>
        /// <returns></returns>
        public bool isMinimized() { return m_isMinimized; }

        /// <summary>
        /// Check if the camera is in the current selected column
        /// </summary>
        /// <returns></returns>
        public bool isSelected()
        {
            return (m_associatedScene.retrieveCurrentSelectedColumnId() == m_idColCamera);
        }

        /// <summary>
        /// Update the culling mask rendered of the camera
        /// </summary>
        /// <param name="cullingMask"></param>
        /// <param name="IRMF"></param>
        public void updateCullingMask(int cullingMask, bool IRMF = false)
        {
            if (IRMF)
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

                if (!m_isMinimized && !m_IRMF)
                {
                    GetComponent<Camera>().cullingMask = m_cullingMask;
                }
            }                
        }

        /// <summary>
        /// Set the minimized state of the camera
        /// </summary>
        /// <param name="state"></param>
        public void setMinimizeState(bool state)
        {
            m_isMinimized = state;
            GetComponent<Camera>().cullingMask = m_isMinimized ? m_minimizedCullingMask : (m_IRMF ? m_IRMFCullingMask : m_cullingMask);
        }

        /// <summary>
        /// Check if the mouse is inside the camera rectangle
        /// </summary>
        /// <returns></returns>
        public bool isFocus()
        {
            return (GetComponent<Camera>().pixelRect.Contains(Input.mousePosition));
        }

        /// <summary>
        /// Check and send the mouse events to the mouse manager and apply cameras rotations and straffes
        /// </summary>
        protected void sendMouveEvents()
        {
            m_isMouseClicking = false;


            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

            // mouse movement
            m_inputsSceneManager.sendMouseMovementToScenes(ray, m_spCamera, Input.mousePosition, m_idColCamera);

            // left click
            if (Input.GetMouseButtonUp(0))
            {
                m_inputsSceneManager.sendClickRayToScenes(ray, m_spCamera, m_idColCamera);
            }

            // right click
            if (Input.GetMouseButton(1))
            {
                m_isMouseClicking = true;
                float nx = 0;
                float ny = 0;
                nx = Input.GetAxis("Mouse X");
                ny = Input.GetAxis("Mouse Y");

                // check horizontal right click mouse drag movement
                if (nx != 0)
                    if (nx < 0)
                        horizontalRotation(true, -nx * m_speed);
                    else 
                        horizontalRotation(false, nx * m_speed);
                
                // check vertical right click mouse drag movement
                if (ny != 0)
                    if (ny < 0)
                        verticalRotation(true,  ny * m_speed);
                    else
                        verticalRotation(false,-ny * m_speed);
            }

            if(Input.GetMouseButton(2))
            {
                m_isMouseClicking = true;
                float nx = 0;
                float ny = 0;
                nx = Input.GetAxis("Mouse X");
                ny = Input.GetAxis("Mouse Y");

                // check horizontal right click mouse drag movement
                if (nx != 0)
                    if (nx < 0)
                        horizontalStrafe(true,  nx * m_speed);
                    else
                        horizontalStrafe(false,-nx * m_speed);


                // check vertical right click mouse drag movement
                if (ny != 0)
                    if (ny < 0)
                        verticalStrafe(true, -ny * m_speed);
                    else
                        verticalStrafe(false, ny * m_speed);
            }

        }

        /// <summary>
        /// Update the selected column with the associated scene
        /// </summary>
        /// <param name="idColumn"></param>
        public void updateSelectedColumn(int idColumn)
        {
            m_associatedScene.updateSelectedColumn(idColumn);
        }


        /// <summary>
        /// Strafe hozizontally the camera position and target with the same vector.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="amount"></param>
        protected void horizontalStrafe(bool left, float amount)
        {
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
        protected void verticalStrafe(bool up, float amount)
        {
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
        protected void horizontalRotation(bool left, float amount)
        {
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
        protected void verticalRotation(bool up, float amount)
        {
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
        protected void moveForward(float amount)
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
        protected void moveBackward(float amount)
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
        protected void resetTarget()
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
        public void defineCamera(Vector3 position, Quaternion rotation, Vector3 target)
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