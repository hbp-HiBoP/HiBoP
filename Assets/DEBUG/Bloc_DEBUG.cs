using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using UnityEngine;
using Tools.CSharp;
using static HBP.Data.Experience.Dataset.EventInformation;
using p = HBP.Data.Experience.Protocol;

public class Bloc_DEBUG : MonoBehaviour
{
    public Texture2D ColorMap;
    public HBP.UI.TrialMatrix.Bloc Bloc;
    public Vector2 Limits;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Test();
        }
    }

    void Test()
    {
        // Set Prefrences.
        ApplicationState.UserPreferences = new HBP.Data.Preferences.UserPreferences();
        ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialSmoothing = false;

        // Main subBloc protocol.
        p.Event mainMainEvent = new p.Event("Main event", new int[] { 1 }, HBP.Data.Enums.MainSecondaryEnum.Main);
        p.SubBloc mainSubBlocProtocol = new p.SubBloc
            ("MainSubBloc",
            0,
            HBP.Data.Enums.MainSecondaryEnum.Main,
            new Window(-500, 500),
            new Window(-200, 0),
            new p.Event[] { mainMainEvent },
            new p.Icon[0], 
            new p.Treatment[0]
            );

        // Secondary subBloc protocol.
        p.Event secondaryMainEvent = new p.Event("Main event", new int[] { 2 }, HBP.Data.Enums.MainSecondaryEnum.Main);
        p.SubBloc secondarySubBlocProtocol = new p.SubBloc
            ("SecondarySubBloc",
            1,
            HBP.Data.Enums.MainSecondaryEnum.Secondary,
            new Window(-500, 500),
            new Window(-200, 0),
            new p.Event[] { secondaryMainEvent },
            new p.Icon[0], 
            new p.Treatment[0]
            );

        // Bloc protocol
        p.Bloc blocProtocol = new p.Bloc("MonBloc", 0, "", "", new p.SubBloc[] { mainSubBlocProtocol, secondarySubBlocProtocol });

        // Main subBloc data
        Dictionary<p.Event, EventInformation> informationByEventForMainSubBloc = new Dictionary<p.Event, EventInformation>();
        EventOccurence mainMainEventOccurence = new EventOccurence(1, 100, 30, 0, 100, 30, 0);
        informationByEventForMainSubBloc.Add(mainMainEvent, new EventInformation(new EventOccurence[] { mainMainEventOccurence }));
        HBP.Data.TrialMatrix.SubTrial[] mainSubTrials = new HBP.Data.TrialMatrix.SubTrial[100];
        for (int i = 0; i < 100; i++)
        {
            float[] values = new float[200];
            for (int p = 0; p < 200; p++)
            {
                values[p] = Random.Range(0, 100);
            }
            ChannelSubTrial channelSubTrial = new ChannelSubTrial(values, true, informationByEventForMainSubBloc);
            mainSubTrials[i] = new HBP.Data.TrialMatrix.SubTrial(channelSubTrial);
        }
        HBP.Data.TrialMatrix.SubBloc mainSubBloc = new HBP.Data.TrialMatrix.SubBloc(mainSubBlocProtocol, mainSubTrials);
        mainSubBloc.SpacesAfter = 10;
        mainSubBloc.SpacesBefore = 50;

        // Secondary subBloc data.
        Dictionary<p.Event, EventInformation> informationByEventForSecondarySubBloc = new Dictionary<p.Event, EventInformation>();
        EventOccurence secondaryMainEventOccurence = new EventOccurence(1, 100, 30, 0, 100, 30, 0);
        informationByEventForSecondarySubBloc.Add(secondaryMainEvent, new EventInformation(new EventOccurence[] { secondaryMainEventOccurence }));
        HBP.Data.TrialMatrix.SubTrial[] secondarySubTrials = new HBP.Data.TrialMatrix.SubTrial[100];
        for (int i = 0; i < 100; i++)
        {
            float[] values = new float[200];
            for (int p = 0; p < 200; p++)
            {
                values[p] = Random.Range(0, 100);
            }
            ChannelSubTrial channelSubTrial = new ChannelSubTrial(values, true, informationByEventForSecondarySubBloc);
            secondarySubTrials[i] = new HBP.Data.TrialMatrix.SubTrial(channelSubTrial);
        }
        HBP.Data.TrialMatrix.SubBloc secondarySubBloc = new HBP.Data.TrialMatrix.SubBloc(secondarySubBlocProtocol, secondarySubTrials);
        secondarySubBloc.SpacesAfter = 10;
        secondarySubBloc.SpacesBefore = 50;

        // Bloc data.
        HBP.Data.TrialMatrix.Bloc blocData = new HBP.Data.TrialMatrix.Bloc(blocProtocol, new HBP.Data.TrialMatrix.SubBloc[] { mainSubBloc, secondarySubBloc });


        //Bloc.Set(blocData, ColorMap, Limits, new Window((int)TimeLimits.x, (int)TimeLimits.y));
    }
}
