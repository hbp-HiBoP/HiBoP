using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HBP.Tests.PlayMode.Utilities
{
    public sealed class PlayModeSceneScope : IDisposable
    {
        private readonly Scene m_PreviousActiveScene;

        public Scene Scene { get; }
        public GameObject Root { get; }

        public PlayModeSceneScope(string namePrefix)
        {
            m_PreviousActiveScene = SceneManager.GetActiveScene();
            Scene = SceneManager.CreateScene($"{namePrefix}-{Guid.NewGuid():N}");
            SceneManager.SetActiveScene(Scene);
            Root = new GameObject("PlayModeTestRoot");
            SceneManager.MoveGameObjectToScene(Root, Scene);
        }

        public Camera CreateCamera(string name = "PlayMode Test Camera")
        {
            GameObject cameraObject = new(name);
            SceneManager.MoveGameObjectToScene(cameraObject, Scene);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 1, -5), Quaternion.identity);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            return camera;
        }

        public Light CreateDirectionalLight(string name = "PlayMode Test Light")
        {
            GameObject lightObject = new(name);
            SceneManager.MoveGameObjectToScene(lightObject, Scene);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
            return light;
        }

        public void Dispose()
        {
            if (m_PreviousActiveScene.IsValid())
            {
                SceneManager.SetActiveScene(m_PreviousActiveScene);
            }

            if (Scene.IsValid())
            {
                foreach (GameObject root in Scene.GetRootGameObjects())
                {
                    if (root != null)
                    {
                        UnityEngine.Object.Destroy(root);
                    }
                }

                SceneManager.UnloadSceneAsync(Scene);
            }
        }
    }
}
