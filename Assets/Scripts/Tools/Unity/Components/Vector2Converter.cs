using Tools.Unity;
using UnityEngine;

public class Vector2Converter : MonoBehaviour
{
    #region Properties
    public FloatEvent OnChangeX;
    public FloatEvent OnChangeY;
    public Vector2Event OnChangeVector2;
    Vector2 m_Value;
    #endregion

    #region Public Methods
    public void SetVector(Vector2 value)
    {
        m_Value = value;
        OnChangeX.Invoke(value.x);
        OnChangeY.Invoke(value.y);
    }
    public void SetX(float value)
    {
        m_Value.x = value;
        OnChangeVector2.Invoke(m_Value);
    }
    public void SetY(float value)
    {
        m_Value.y = value;
        OnChangeVector2.Invoke(m_Value);
    }
    #endregion

    #region Private Methods
    private void Start()
    {
        
    }
    #endregion
}
