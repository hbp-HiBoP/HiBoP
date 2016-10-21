//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//public class ElectrodesGroup : MonoBehaviour
//{
//    public GameObject[] plotsArray;
//    public GameObject combinedObj;



//    public void combinePlots()
//    {
//        List<CombineInstance> plotList = new List<CombineInstance>();

//        for (int ii = 0; ii < plotsArray.Length; ii++)
//        {
//            GameObject currentPlot = plotsArray[ii];
            


//            MeshFilter meshFilter = currentPlot.GetComponent<MeshFilter>();
//            CombineInstance combine = new CombineInstance();

//            combine.mesh = meshFilter.mesh;
//            combine.transform = meshFilter.transform.localToWorldMatrix;

//            currentPlot.SetActive(false);

//            //Add it to the list of leaf mesh data
//            plotList.Add(combine);
//            //Debug.Log("add mesh " + combine.mesh.vertexCount);
//        }


//        Mesh combinedPlotMesh = new Mesh();
//        combinedPlotMesh.CombineMeshes(plotList.ToArray(), false);



//        combinedObj = new GameObject("test");
//        combinedObj.AddComponent<MeshRenderer>();
//        combinedObj.AddComponent<MeshFilter>();
//        combinedObj.GetComponent<MeshFilter>().mesh = combinedPlotMesh;
//        Debug.Log("combinedPlotMesh " + combinedPlotMesh.vertexCount);


//    }

//	// Use this for initialization
//	void Start ()
//    {
	
//	}
	
//	// Update is called once per frame
//	void Update () {
	
//	}
//}
