using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class testCombine : MonoBehaviour {

    
    public GameObject[] cubesArray;
    //The object that is going to hold the combined mesh
    //public GameObject combinedObj;

    // Use this for initialization
    void Start () {

        Debug.Log("start");
        List<CombineInstance> cubesList = new List<CombineInstance>();

        cubesArray = new GameObject[transform.childCount];
        for (int i = 0; i < cubesArray.Length; i++)
        {
            cubesArray[i] = transform.GetChild(i).gameObject;
        }


        Debug.Log("cubes : " + cubesArray.Length);

        for (int ii = 0; ii < cubesArray.Length; ++ii)
        {
            GameObject currentCube = cubesArray[ii];
            CombineInstance combine = new CombineInstance();

            //Deactivate the tree 
            currentCube.SetActive(false);

            //Get all meshfilters from this tree, true to also find deactivated children
            //MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

            MeshFilter meshFilter = currentCube.GetComponent<MeshFilter>();

            combine.mesh = meshFilter.mesh;
            combine.transform = meshFilter.transform.localToWorldMatrix;
            cubesList.Add(combine);
        }

        Mesh combinedCubeMesh = new Mesh();
        combinedCubeMesh.CombineMeshes(cubesList.ToArray());

        GetComponent<MeshFilter>().mesh = combinedCubeMesh;



    }
	
	// Update is called once per frame
	void Update ()
    {
        //List<CombineInstance> cubesList = new List<CombineInstance>();

        //cubesArray = new GameObject[transform.childCount];
        //for (int i = 0; i < cubesArray.Length; i++)
        //{
        //    cubesArray[i] = transform.GetChild(i).gameObject;
        //}

        //for (int ii = 0; ii < cubesArray.Length; ++ii)
        //{
        //    GameObject currentCube = cubesArray[ii];
        //    CombineInstance combine = new CombineInstance();

        //    //Deactivate the tree 
        //    currentCube.SetActive(false);

        //    //Get all meshfilters from this tree, true to also find deactivated children
        //    //MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

        //    MeshFilter meshFilter = currentCube.GetComponent<MeshFilter>();

        //    combine.mesh = meshFilter.mesh;
        //    combine.transform = meshFilter.transform.localToWorldMatrix;
        //    cubesList.Add(combine);
        //}

        //Mesh combinedCubeMesh = new Mesh();
        //combinedCubeMesh.CombineMeshes(cubesList.ToArray());

        //GetComponent<MeshFilter>().mesh = combinedCubeMesh;
    }
}
