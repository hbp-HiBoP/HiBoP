using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(Selectable))]
    public class NavigationGestion : MonoBehaviour
    {
        #region Properties
        public Selectable Selectable { get; private set; }
        public enum DirectionType { Up, Down, Left, Right}
        public DirectionType Direction;
        #endregion

        #region Public Methods
        public void SelectNext()
        {
            Selectable next = null;
            switch (Direction)
            {
                case DirectionType.Up:
                    next = Selectable.FindSelectableOnUp();
                    break;
                case DirectionType.Down:
                    next = Selectable.FindSelectableOnDown();
                    break;
                case DirectionType.Left:
                    next = Selectable.FindSelectableOnLeft();
                    break;
                case DirectionType.Right:
                    next = Selectable.FindSelectableOnRight();
                    break;
            }
            next?.Select();
        }
        #endregion

        #region Private Methods
        // Start is called before the first frame update
        void Start()
        {
            Selectable = GetComponent<Selectable>();
        }

        // Update is called once per frame
        void Update()
        {
        }
        #endregion

    }
}