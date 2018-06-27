using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewTheme
{
    [CreateAssetMenu(menuName = "Theme/Settings/Camera")]
    public class CameraBackground : Settings
    {
        public UnityEngine.Color Color;

        public override void Set(GameObject gameObject)
        {
            Camera camera = gameObject.GetComponent<Camera>();
            if (camera)
            {
                camera.backgroundColor = Color;
            }
        }
    }
}