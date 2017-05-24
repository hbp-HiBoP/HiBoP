using UnityEngine;
using Tools.Unity;

public class DialogBoxManager : MonoBehaviour
{
    #region Properties
    [SerializeField, Candlelight.PropertyBackingField]
    private GameObject m_InformationAlertPrefab;
    public GameObject InformationAlertPrefab
    {
        get { return m_InformationAlertPrefab; }
        set { m_InformationAlertPrefab = value; }
    }

    [SerializeField, Candlelight.PropertyBackingField]
    private GameObject m_WarningAlertPrefab;
    public GameObject WarningAlertPrefab
    {
        get { return m_WarningAlertPrefab; }
        set { m_WarningAlertPrefab = value; }
    }

    [SerializeField, Candlelight.PropertyBackingField]
    private GameObject m_ErrorAlertPrefab;
    public GameObject ErrorAlertPrefab
    {
        get { return m_ErrorAlertPrefab; }
        set { m_ErrorAlertPrefab = value; }
    }

    [SerializeField, Candlelight.PropertyBackingField]
    private Canvas m_Canvas;
    public Canvas Canvas
    {
        get { return m_Canvas; }
        set { m_Canvas = value; }
    }

    public enum AlertType { Informational, Warning, Error}
    #endregion

    #region Public Methods
    public void Open(AlertType type,string message)
    {
        GameObject dialogBox;
        switch (type)
        {
            case AlertType.Informational:
                dialogBox = Instantiate(InformationAlertPrefab, Canvas.transform);
                break;
            case AlertType.Warning:
                dialogBox = Instantiate(WarningAlertPrefab, Canvas.transform);
                break;
            case AlertType.Error:
                dialogBox = Instantiate(ErrorAlertPrefab, Canvas.transform);
                break;
            default:
                dialogBox = Instantiate(InformationAlertPrefab, Canvas.transform);
                break;
        }
        dialogBox.transform.SetAsLastSibling();
        dialogBox.GetComponent<DialogBox>().Open(message);
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.A))
        {
            Open(AlertType.Informational, "Hello world!");
        }
        if (Input.GetKey(KeyCode.Z))
        {
            Open(AlertType.Informational, "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quam felis, ultricies nec, pellentesque eu, pretium quis, sem. Nulla consequat massa quis enim. Donec pede justo, fringilla vel, aliquet nec, vulputate eget, arcu. In enim justo, rhoncus ut, imperdiet a, venenatis vitae, justo. Nullam dictum felis eu pede mollis pretium. Integer tincidunt. Cras dapibus. Vivamus elementum semper nisi. Aenean vulputate eleifend tellus. Aenean leo ligula, porttitor eu, consequat vitae, eleifend ac, enim. Aliquam lorem ante, dapibus in, viverra quis, feugiat a, tellus. Phasellus viverra nulla ut metus varius laoreet. Quisque rutrum. Aenean imperdiet. Etiam ultricies nisi vel augue. Curabitur ullamcorper ultricies nisi. Nam eget dui. Etiam rhoncus. Maecenas tempus, tellus eget condimentum rhoncus, sem quam semper libero, sit amet adipiscing sem neque sed ipsum. Nam quam nunc, blandit vel, luctus pulvinar, hendrerit id, lorem. Maecenas nec odio et ante tincidunt tempus. Donec vitae sapien ut libero venenatis faucibus. Nullam quis ante. Etiam sit amet orci eget eros faucibus tincidunt. Duis leo. Sed fringilla mauris sit amet nibh. Donec sodales sagittis magna. Sed consequat, leo eget bibendum sodales, augue velit cursus nunc, ");
        }
    }
    #endregion
}
