using UnityEngine;

namespace HBP.UI
{
    public class OnPointerEnterCanvasHandler : MonoBehaviour
    {
        [SerializeField]
        HBP.UI.HBPLinker m_Module3DLinker;

        public void Enter()
        {
            m_Module3DLinker.Command3DModule.setModuleFocusState(false);
        }

        public void Out()
        {
            m_Module3DLinker.Command3DModule.setModuleFocusState(true);
        }
    }
}

