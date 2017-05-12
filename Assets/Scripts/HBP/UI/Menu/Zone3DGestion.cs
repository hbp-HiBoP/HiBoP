using UnityEngine;
using HBP.Module3D;
using UnityEngine.EventSystems;

namespace HBP.UI
{
    [ExecuteInEditMode]
    public class Zone3DGestion : MonoBehaviour
    {
        #region Properties
        HiBoP_3DModule_API command;
        Camera backGroundCamera;
        #endregion

        #region Events
        public void OnPointerEnter()
        {
            command.SetModuleFocus(false);
        }
        public void OnPointerExit()
        {
            command.SetModuleFocus(true);
        }
        void OnRectTransformDimensionsChange()
        {
            Update3DPanel();
        }
        void OnColumnsChange(bool sp, bool[] columns)
        {
            if (sp)
            {
                VisualizationLoaded.SP_Columns = columns;
            }
            else
            {
                VisualizationLoaded.MP_Columns = columns;
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            command = FindObjectOfType<HiBoP_3DModule_API>();
            command.UpdateColumnMinimizedState.AddListener((sp, columns) => OnColumnsChange(sp, columns.ToArray()));
            AddEvents();
        }
        void Update3DPanel()
        {
            if (!backGroundCamera) backGroundCamera = GameObject.Find("Background camera").GetComponent<Camera>();
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