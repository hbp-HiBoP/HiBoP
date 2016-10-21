using UnityEngine;
using System.Collections;

namespace HBP.VISU3D.TESTS
{

    public class testShaders : MonoBehaviour
    {

        public float speed = 100f;

        // Use this for initialization
        void Start()
        {

            Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = new Vector2[vertices.Length];
            Vector2[] uvs1 = new Vector2[vertices.Length];
            Vector2[] uvs2 = new Vector2[vertices.Length];
            int i = 0;
            while (i < uvs.Length)
            {
                uvs[i] = new Vector2(0.8f, 0.5f);
                uvs1[i] = new Vector2(0.9f, 0.5f);
                uvs2[i] = new Vector2(0.8f, 0.8f);
                //uvs1[i] = new Vector2(14f, 2f);
                i++;
            }

            uvs1[0] = new Vector2(0.2f, 0.5f);
            uvs1[1] = new Vector2(0.0f, 0.5f);
            uvs1[7] = new Vector2(0.6f, 0.5f);

            uvs2[0] = new Vector2(0.2f, 0.8f);

            mesh.uv = uvs;
            mesh.uv2 = uvs1;
            mesh.uv3 = uvs2;
            //mesh.uv4 = uvs;


        }

        // Update is called once per frame
        void Update()
        {

            float t = Time.deltaTime;

            transform.Rotate(Vector3.left, speed * t);
            //transform.Rotate(Vector3.left, speed * t*0.5f);
        }
    }
}