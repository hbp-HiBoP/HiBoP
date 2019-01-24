using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    public class MousePositionRatio : MonoBehaviour
    {
        #region Properties
        public RectTransform Container;

        [SerializeField] Vector2 position;
        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                if(position != value)
                {
                    position = value;
                    OnChangePosition.Invoke(value);
                }
            }
        }
        public Vector2Event OnChangePosition = new Vector2Event();
        #endregion

        #region Private Methods
        private void Update()
        {
            Position = Container.GetRatioPosition(Input.mousePosition);
        }
        #endregion
    }
}