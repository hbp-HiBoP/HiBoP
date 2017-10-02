using UnityEngine;

public class Test : MonoBehaviour
{
    public HBP.UI.Experience.Dataset.DataInfoModifier m_DataInfoModifier;

	// Use this for initialization
	void Start ()
    {
        HBP.Data.Experience.Dataset.DataInfo dataInfo = new HBP.Data.Experience.Dataset.DataInfo("Name",new HBP.Data.Patient(),"Measure","EEG","POS",new HBP.Data.Experience.Protocol.Protocol());
        m_DataInfoModifier.Open(dataInfo, true);
	}
}
