using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HBP.Tests.PlayMode.Utilities
{
    public sealed class PlayModeWindowHarness
    {
        public Canvas Canvas { get; }
        public EventSystem EventSystem { get; }
        public GameObject Root { get; }

        public PlayModeWindowHarness(Scene scene, string rootName)
        {
            Root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(Root, scene);

            Canvas = Root.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Root.AddComponent<CanvasScaler>();
            Root.AddComponent<GraphicRaycaster>();

            GameObject eventSystemObject = new($"{rootName} EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            EventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
