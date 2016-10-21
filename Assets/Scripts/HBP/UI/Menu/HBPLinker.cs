using UnityEngine;

namespace HBP.UI
{
    public class HBPLinker : MonoBehaviour
    {
        [SerializeField]
        VISU3D.HBP_3DModule_Command m_command;
        public VISU3D.HBP_3DModule_Command Command3DModule { get { return m_command; } }

        [SerializeField]
        Camera m_camera;
        public Camera BackgroundCamera { get { return m_camera; } }
    }
}