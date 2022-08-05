using UnityEngine;

namespace HBP.Theme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Camera")]
    public class CameraBackground : Settings
    {
        public Color Color;

        public override void Set(GameObject gameObject)
        {
            Camera camera = gameObject.GetComponent<Camera>();
            if (camera)
            {
                camera.backgroundColor = Color.Value;
            }
        }
    }
}