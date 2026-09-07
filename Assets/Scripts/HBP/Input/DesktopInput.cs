using UnityEngine;
using UnityEngine.InputSystem;

namespace HBP.Input
{
    /// <summary>
    /// Desktop device reads with neutral values when a device is absent.
    /// Devices are resolved on every access to follow replacement and reconnection.
    /// UI event deltas and contextual shortcut rules remain with their consumers.
    /// </summary>
    public static class DesktopInput
    {
        public static Vector3 MousePosition => Mouse.current?.position.ReadValue() ?? Vector2.zero;
        public static Vector2 MouseDelta => Mouse.current?.delta.ReadValue() ?? Vector2.zero;
        public static Vector2 ScrollDelta => Mouse.current?.scroll.ReadValue() ?? Vector2.zero;

        public static bool IsLeftMouseButtonPressed => Mouse.current?.leftButton.isPressed == true;
        public static bool IsRightMouseButtonPressed => Mouse.current?.rightButton.isPressed == true;
        public static bool IsMiddleMouseButtonPressed => Mouse.current?.middleButton.isPressed == true;
        public static bool WasLeftMouseButtonPressedThisFrame => Mouse.current?.leftButton.wasPressedThisFrame == true;
        public static bool WasLeftMouseButtonReleasedThisFrame => Mouse.current?.leftButton.wasReleasedThisFrame == true;

        public static bool IsAnyKeyPressed => Keyboard.current?.anyKey.isPressed == true;
        public static bool WasAnyKeyPressedThisFrame => Keyboard.current?.anyKey.wasPressedThisFrame == true;
        public static bool IsControlPressed => IsPressed(Key.LeftCtrl) || IsPressed(Key.RightCtrl);
        public static bool IsShiftPressed => IsPressed(Key.LeftShift) || IsPressed(Key.RightShift);
        public static bool IsAltPressed => IsPressed(Key.LeftAlt) || IsPressed(Key.RightAlt);

        public static bool IsPressed(Key key) => Keyboard.current?[key].isPressed == true;
        public static bool WasPressedThisFrame(Key key) => Keyboard.current?[key].wasPressedThisFrame == true;
    }
}
