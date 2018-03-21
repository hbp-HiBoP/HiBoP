using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tools.Unity.Graph;

public class DEBUG : MonoBehaviour
{
    public SimplifiedGraph simplifiedGraph;

	void Start ()
    {
        GroupCurveData group = new GroupCurveData("testGroupCurveData");

        Vector2[] ROIpoints = new Vector2[]
        {
        new Vector2(0,1),
        new Vector2(1,2),
        new Vector2(2,2),
        new Vector2(3,3),
        new Vector2(4,5),
        new Vector2(5,0)
        };
        CurveData ROI = new CurveData("ROI", "ROI", ROIpoints, Color.red);

        Vector2[] points = new Vector2[]
        {
        new Vector2(0,3),
        new Vector2(1,5),
        new Vector2(2,4),
        new Vector2(3,0),
        new Vector2(4,2),
        new Vector2(5,0)
        };
        CurveData Normal = new CurveData("Normal", "Normal", points, Color.cyan);

        group.Curves = new List<CurveData> { Normal, ROI };

        GraphData myGraph = new GraphData("test", "abscissa", "ordinate", Color.black, Color.white, new GroupCurveData[] { group }, new Limits(0,5,0,5));
        simplifiedGraph.Plot(myGraph);
    }

}
