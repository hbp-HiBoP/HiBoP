using HBP.Data.Experience.Dataset;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HBP.Data.Experience.Dataset.EventInformation;

public class SubBloc_DEBUG : MonoBehaviour
{
    public Texture2D ColorMap;
    public HBP.UI.TrialMatrix.SubBloc SubBloc;
    public Vector2 Limits;
    public Vector2 TimeLimits;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            Test();
        }
    }

    void Test()
    {
        ApplicationState.UserPreferences = new HBP.Data.Preferences.UserPreferences();
        ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialSmoothing = false;
        HBP.Data.Experience.Protocol.Event _event = new HBP.Data.Experience.Protocol.Event("Main event", new int[] { 1 }, HBP.Data.Enums.MainSecondaryEnum.Main);
        HBP.Data.Experience.Protocol.SubBloc subBlocProtocol = new HBP.Data.Experience.Protocol.SubBloc
            ("MonSubBloc",
            0,
            HBP.Data.Enums.MainSecondaryEnum.Main,
            new Tools.CSharp.Window(-500,500),
            new Tools.CSharp.Window(-200,0),
            new HBP.Data.Experience.Protocol.Event[] { _event },
            new HBP.Data.Experience.Protocol.Icon[0], new HBP.Data.Experience.Protocol.Treatment[0]
            );
        Dictionary<HBP.Data.Experience.Protocol.Event, EventInformation> informationByEvent = new Dictionary<HBP.Data.Experience.Protocol.Event, EventInformation>();
        EventOccurence occurence = new EventOccurence(1, 100, 30, 0, 100, 30, 0);
        informationByEvent.Add(_event, new EventInformation(new EventOccurence[] { occurence }));
        HBP.Data.TrialMatrix.SubTrial[] subTrials = new HBP.Data.TrialMatrix.SubTrial[100];
        for (int i = 0; i < 100; i++)
        {
            float[] values = new float[200];
            for (int p = 0; p < 200; p++)
            {
                values[p] = Random.Range(0,100);
            }
            ChannelSubTrial channelSubTrial = new ChannelSubTrial(values, true, informationByEvent);
            subTrials[i] = new HBP.Data.TrialMatrix.SubTrial(channelSubTrial);
        }

        HBP.Data.TrialMatrix.SubBloc data = new HBP.Data.TrialMatrix.SubBloc(subBlocProtocol, subTrials);
        data.SpacesAfter = 10;
        data.SpacesBefore = 50;
        SubBloc.Set(data, ColorMap, Limits, new Tools.CSharp.Window((int)TimeLimits.x, (int)TimeLimits.y));
    }
}
