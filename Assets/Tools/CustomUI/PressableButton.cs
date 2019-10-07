using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    /// <summary>
    /// Class extending the Button class to add onPress and onRelease events
    /// </summary>
    public class PressableButton : Button
    {
        #region Events
        /// <summary>
        /// Event called when pressing the button
        /// </summary>
        public UnityEvent onPress = new UnityEvent();
        /// <summary>
        /// Event called when releasing the button
        /// </summary>
        public UnityEvent onRelease = new UnityEvent();
        #endregion

        #region Public Methods
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            onPress.Invoke();
        }
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            onRelease.Invoke();
        }
        #endregion
    }
}