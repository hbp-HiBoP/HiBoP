using System;

namespace UnityEngine.Events
{
    [Serializable] public class BoolEvent : UnityEvent<Boolean> { }
    [Serializable] public class FloatEvent : UnityEvent<float> { }
    [Serializable] public class IntEvent : UnityEvent<int> { }
    [Serializable] public class ColorEvent : UnityEvent<Color> { }
    [Serializable] public class Vector2Event : UnityEvent<Vector2> { }
    [Serializable] public class Vector2ArrayEvent : UnityEvent<Vector2[]> { }
    [Serializable] public class WindowArrayEvent : UnityEvent<HBP.Core.Tools.TimeWindow[]> { }
    [Serializable] public class StringEvent : UnityEvent<String> { }
    [Serializable] public class Texture2DEvent : UnityEvent<Texture2D> { }
    [Serializable] public class SavableWindowEvent : UnityEvent<HBP.UI.DialogWindow> { }
}