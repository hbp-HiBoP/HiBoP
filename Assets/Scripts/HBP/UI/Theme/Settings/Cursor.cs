using UnityEngine;

namespace Theme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Cursor")]
    public class Cursor : Settings
    {
        public Texture2D SourceImage;
        public Vector2 Hotspot;

        public override void Set(GameObject gameObject)
        {
            if (gameObject.activeSelf && !Input.GetMouseButton(0)) UnityEngine.Cursor.SetCursor(SourceImage, Hotspot, CursorMode.Auto);
        }
    }
}