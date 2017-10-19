using UnityEngine.UI;

public class ToggleExtension : Toggle
{
    public void ChangeState()
    {
        isOn = !isOn;
    }
}
