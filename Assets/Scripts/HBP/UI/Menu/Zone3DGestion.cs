using UnityEngine;
using HBP.VISU3D;
using UnityEngine.EventSystems;

namespace HBP.UI
{
    public class Zone3DGestion : MonoBehaviour
    {
        #region Properties
        HBP_3DModule_Command command;
        Camera backGroundCamera;
        #endregion

        #region Events
        public void OnPointerEnter()
        {
            command.setModuleFocusState(false);
        }
        public void OnPointerExit()
        {
            command.setModuleFocusState(true);
        }
        void OnRectTransformDimensionsChange()
        {
            Update3DPanel();
        }
        void OnColumnsChange(bool sp, bool[] columns)
        {
            if (sp)
            {
                VisualisationLoaded.SP_Columns = columns;
            }
            else
            {
                VisualisationLoaded.MP_Columns = columns;
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            command = FindObjectOfType<HBP_3DModule_Command>();
            command.UpdateColumnMinimizeStateEvent.AddListener((sp, columns) => OnColumnsChange(sp, columns.ToArray()));
            AddEvents();
        }
        void Update3DPanel()
        {
            if (!backGroundCamera) backGroundCamera = GameObject.Find("background camera").GetComponent<Camera>();
            backGroundCamera.pixelRect = (transform as RectTransform).rect;
        }
        void AddEvents()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            EventTrigger eventTrigger = canvas.GetComponent<EventTrigger>();
            if (!eventTrigger)
            {
                eventTrigger = canvas.gameObject.AddComponent<EventTrigger>();
            }
            eventTrigger.hideFlags = HideFlags.HideInInspector;

            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => OnPointerEnter());
            eventTrigger.triggers.Add(pointerEnter);

            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => OnPointerExit());
            eventTrigger.triggers.Add(pointerExit);
        }
        #endregion
    }
}