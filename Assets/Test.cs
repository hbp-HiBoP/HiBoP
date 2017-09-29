using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public HBP.UI.Experience.Protocol.BlocModifier m_BlocModifier;

	// Use this for initialization
	void Start ()
    {
        HBP.Data.Experience.Protocol.DisplayInformations displayInformations = new HBP.Data.Experience.Protocol.DisplayInformations(0, 0, "Test", "", "C0L1", new Vector2(-200, 200), new Vector2(-200, 0));
        HBP.Data.Experience.Protocol.Bloc bloc = new HBP.Data.Experience.Protocol.Bloc(displayInformations);
        m_BlocModifier.Open(bloc, true);
	}
}
