using UnityEngine;

namespace Tools.Unity.Components
{
    public class CursorWrapper : MonoBehaviour
    {
        public bool Visible
        {
            get
            {
                return Cursor.visible;
            }
            set
            {
                Cursor.visible = value;
            }
        }
    }
}