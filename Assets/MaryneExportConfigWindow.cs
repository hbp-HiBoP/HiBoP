using HBP.UI.Tools;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MaryneExportConfigWindow : DialogWindow
{
    [SerializeField] InputField m_ProtocolsInputField;
    [SerializeField] InputField m_AreasInputField;
    [SerializeField] InputField m_DataTypesInputField;
    [SerializeField] Button m_ResetButton;

    public GenericEvent<string[], string[], string[]> OnConfigurationChanged = new();

    private string[] m_DefaultProtocols = new string[] { "VISU", "LEC1" };
    private string[] m_DefaultAreas = new string[] { "CTX_OCCIPITAL", "HIPPOCAMP", "HNP", "SB", "CTX_PARIETAL", "CTX_TEMPORAL", "CTX_FRONTAL", "CTX_OF", "CTX_MOTEUR" };
    private string[] m_DefaultDataTypes = new string[] { "f50f150sm0" };

    protected override void Initialize()
    {
        base.Initialize();
        m_ResetButton.onClick.AddListener(SetDefaultValues);
    }

    protected override void SetFields()
    {
        base.SetFields();
        SetDefaultValues();
    }

    private void SetDefaultValues()
    {
        m_ProtocolsInputField.text = string.Join(",", m_DefaultProtocols);
        m_AreasInputField.text = string.Join(",", m_DefaultAreas);
        m_DataTypesInputField.text = string.Join(",", m_DefaultDataTypes);
    }

    public override void OK()
    {
        string[] protocols = ParseInputField(m_ProtocolsInputField?.text);
        string[] areas = ParseInputField(m_AreasInputField?.text);
        string[] dataTypes = ParseInputField(m_DataTypesInputField?.text);

        if (protocols.Length == 0 || areas.Length == 0 || dataTypes.Length == 0)
        {
            DialogBoxManager.Open(DialogBoxManager.AlertType.Warning, "Incomplete configuration", "Please fill all fields with at least one value.");
            return;
        }

        OnConfigurationChanged?.Invoke(protocols, areas, dataTypes);
        base.OK();
    }

    private string[] ParseInputField(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new string[0];

        string[] values = input.Split(',');
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = values[i].Trim();
        }
        return values.Where(v => !string.IsNullOrEmpty(v)).ToArray();
    }

    public void SetCurrentConfiguration(string[] protocols, string[] areas, string[] dataTypes)
    {
        m_ProtocolsInputField.text = string.Join(",", protocols);
        m_AreasInputField.text = string.Join(",", areas);
        m_DataTypesInputField.text = string.Join(",", dataTypes);
    }
}