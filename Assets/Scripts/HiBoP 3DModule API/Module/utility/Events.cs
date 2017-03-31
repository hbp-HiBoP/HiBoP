/**
 * \file    Events.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UIEvent/UIEvent_Click
 */

// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.VISU3D
{
    /// <summary>
    ///  UI event with no parameters
    /// </summary>
    public class NoParamEvent : UnityEvent { }

    /// <summary>
    /// UI event transmetting a bool
    /// </summary>
    public class SendBoolValueEvent : UnityEvent<bool> { }

    /// <summary>
    /// UI event transmetting an int
    /// </summary>
    public class SendIntValueEvent : UnityEvent<int> { }

    /// <summary>
    /// UI event transmetting a string
    /// </summary>
    public class SendStringValueEvent : UnityEvent<string> { }

    /// <summary>
    /// Event for sending a float associated to a column id
    /// </summary>
    public class SendColumnFloatEvent : UnityEvent<float, int> { }

    /// <summary>
    /// UI event for mouse click with ray parameter and type of scene
    /// </summary>
    public class UIEvent_Click : UnityEvent<Ray, bool> { }

    /// <summary>
    /// UI event for mouse movement with ray parameter, type of scene and id column
    /// </summary>
    public class UIEvent_MouseMovement : UnityEvent<Ray, bool, Vector3, int> { }
}