using System.Collections.Generic;
using UnityEngine;
using Tools.Unity.Graph;

public class DEBUG : MonoBehaviour
{
    public GraphList graphList;
    public SimplifiedGraph simplifiedGraph;
    int nbPoints = 101;
    GraphData graph;
    GroupCurveData group;

	void Start ()
    {
        Vector2[] ROIpoints = new Vector2[nbPoints];
        for (int i = 0; i < nbPoints; i++)
        {
            ROIpoints[i] = new Vector2(i, 50);
        }
        CurveData ROI = new CurveData("ROI", "ROI", ROIpoints, Color.red,1.5f);

        Vector2[] points = new Vector2[nbPoints];
        for (int p = 0; p < nbPoints; p++)
        {
            points[p] = new Vector2(p, p);
        }
        CurveData Normal = new CurveData("Normal", "Normal", points, Color.yellow, 1.5f);
        group = new GroupCurveData("testGroupCurveData");
        group.Curves = new List<CurveData> { Normal, ROI };
        graph = new GraphData("site n°" + 0, "abscissa", "ordinate", Color.black, Color.white, new GroupCurveData[] { group }, new Limits(0, 100, 0, 100));

    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.D))
        {
            GraphData[] graphs = new GraphData[50];
            for (int i = 0; i < 50; i++)
            {
                graphs[i] = new GraphData("A'" + (i + 1), "abscissa", "ordinate", Color.black, Color.white, new GroupCurveData[] { group }, new Limits(0, 100, 0, 100));
            }
            graphList.Display(graphs);
            //simplifiedGraph.Plot(graph);
        }
    }
}
