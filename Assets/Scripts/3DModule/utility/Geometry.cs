

/**
 * \file    Geometry.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Plane, BBox and Geometry classes
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// BBox simple class with a min and max 3D points
    /// </summary>
    public struct BBox
    {
        /// <summary>
        ///  Constructor of the bounding box
        /// </summary>
        /// <param name="bboxArray"></param>
        public BBox(float[] bboxArray)
        {
            this.min = new Vector3(bboxArray[0], bboxArray[1], bboxArray[2]);
            this.max = new Vector3(bboxArray[3], bboxArray[4], bboxArray[5]);
        }

        public Vector3 min; /**< min point of the bBox */
        public Vector3 max; /**< max point of the bBox */
    }

    [System.Serializable]
    /// <summary>
    /// A simple plane class using a normal and a point
    /// </summary>
    public class Plane
    {
        /// <summary>
        /// Plane default constructor
        /// </summary>
        public Plane()
        {
            this.point = new Vector3(0, 0, 0);
            this.normal = new Vector3(1, 0, 0);
        }

        /// <summary>
        /// Plane constructor
        /// </summary>
        /// <param name="point"></param>
        /// <param name="normal"></param>
        public Plane(Vector3 point, Vector3 normal)
        {
            this.point = point;
            this.normal = normal;
        }

        /// <summary>
        /// Convert to float array for DLL use
        /// </summary>
        /// <returns></returns>
        public float[] convertToArray()
        {
            return new float[] { point[0], point[1], point[2], normal[0], normal[1], normal[2]};
        }

        public Vector3 point; /**< point on the plane */
        public Vector3 normal; /**< normal of the plane */
    }

    public class Geometry
    {
        public static Vector3[] create3DCirclePoints(Vector3 center, float ray, int nbVerticesOnCircle)
        {
            Vector3[] verts = new Vector3[nbVerticesOnCircle];
            float angle = 360.0f / (float)(verts.Length - 1);

            for (int ii = 0; ii < verts.Length; ++ii)
            {
                verts[ii] = center + Quaternion.AngleAxis(angle * (float)(ii - 1), Vector3.back) * Vector3.up * ray;
            }

            return verts;
        }

        //public static Vector3[] create3DCirclePoints(Vector3 center, Vector3 normal, float ray, int nbVerticesOnCircle)
        //{
        //    Vector3[] verts = new Vector3[nbVerticesOnCircle];
        //    float t = 2f * (float)Math.PI / nbVerticesOnCircle;
        //    for (int ii = 0; ii < verts.Length; ++ii)
        //    {
        //        verts[ii] = new Vector3(ray * (float)Math.Cos(t * ii), 0, -ray * (float)Math.Sin(t * ii));
        //    }

        //    var up = new Vector3(0, 1, 0);
        //    // Set normal length to 1
        //    normal.Normalize();
        //    var axis = Vector3.Cross(up, normal); // Cross product is rotation axis

        //    Quaternion.an
        //    var angle = Vector3.AngleBetween(up, normal); // Angle to rotate
        //    trn.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(axis, angle)));
        //    trn.Children.Add(new TranslateTransform3D(new Vector3D(center.X, center.Y, center.Z)))



        //    return verts;
        //}


        public static Mesh createSphereMesh(float radius, int nbLong = 24, int nbLat = 16)
        {
            Mesh mesh = new Mesh();

            #region Vertices
            Vector3[] vertices = new Vector3[(nbLong + 1) * nbLat + 2];
            float _pi = Mathf.PI;
            float _2pi = _pi * 2f;

            vertices[0] = Vector3.up * radius;
            for (int lat = 0; lat < nbLat; lat++)
            {
                float a1 = _pi * (float)(lat + 1) / (nbLat + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= nbLong; lon++)
                {
                    float a2 = _2pi * (float)(lon == nbLong ? 0 : lon) / nbLong;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[lon + lat * (nbLong + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
                }
            }
            vertices[vertices.Length - 1] = Vector3.up * -radius;
            #endregion

            #region Normales		
            Vector3[] normales = new Vector3[vertices.Length];
            for (int n = 0; n < vertices.Length; n++)
                normales[n] = vertices[n].normalized;
            #endregion

            #region UVs
            //Vector2[] uvs = new Vector2[vertices.Length];
            //for(int ii = 0; ii < uvs.Length; ++ii)
            //{
            //    uvs[ii] = new Vector2(0.1f, 0.1f);
            //}
            //uvs[0] = Vector2.up;
            //uvs[uvs.Length - 1] = Vector2.zero;
            //for (int lat = 0; lat < nbLat; lat++)
            //    for (int lon = 0; lon <= nbLong; lon++)
            //        uvs[lon + lat * (nbLong + 1) + 1] = new Vector2((float)lon / nbLong, 1f - (float)(lat + 1) / (nbLat + 1));
            #endregion

            #region Triangles
            int nbFaces = vertices.Length;
            int nbTriangles = nbFaces * 2;
            int nbIndexes = nbTriangles * 3;
            int[] triangles = new int[nbIndexes];

            //Top Cap
            int i = 0;
            for (int lon = 0; lon < nbLong; lon++)
            {
                triangles[i++] = lon + 2;
                triangles[i++] = lon + 1;
                triangles[i++] = 0;
            }

            //Middle
            for (int lat = 0; lat < nbLat - 1; lat++)
            {
                for (int lon = 0; lon < nbLong; lon++)
                {
                    int current = lon + lat * (nbLong + 1) + 1;
                    int next = current + nbLong + 1;

                    triangles[i++] = current;
                    triangles[i++] = current + 1;
                    triangles[i++] = next + 1;

                    triangles[i++] = current;
                    triangles[i++] = next + 1;
                    triangles[i++] = next;
                }
            }

            //Bottom Cap
            for (int lon = 0; lon < nbLong; lon++)
            {
                triangles[i++] = vertices.Length - 1;
                triangles[i++] = vertices.Length - (lon + 2) - 1;
                triangles[i++] = vertices.Length - (lon + 1) - 1;
            }
            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            //mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();

            return mesh;
        }

        public static void displayNormalsDebug(GameObject obj)
        {
            Vector3[] normals = obj.GetComponent<MeshFilter>().mesh.normals;
            Vector3[] vertices = obj.GetComponent<MeshFilter>().mesh.vertices;

            if (normals.Length != vertices.Length)
            {
                Debug.LogError("-ERROR: Geometry::displayNormalsDebug -> bad number of normals");
                return;
            }

            for (int ii = 0; ii < normals.Length; ++ii)
            {
                Vector3 invPos = obj.transform.position + vertices[ii];
                invPos.x = -invPos.x;
                Vector3 norm = normals[ii];
                norm.x = -norm.x;

                Debug.DrawRay(invPos, 3 * norm, Color.green);
            }
        }

        public static void displayBBoxDebug(DLL.BBox bbox, Vector3 offset)
        {
            List<Vector3> linesPoints = bbox.linesPairPoints();

            for (int ii = 0; ii < linesPoints.Count; ii += 2)
            {
                Debug.DrawRay(offset + linesPoints[ii], linesPoints[ii + 1] - linesPoints[ii], Color.red);
            }
        }

        public static void displayBBoxPlaneIntersec(DLL.BBox bbox, Plane plane, Vector3 offset)
        {
            List<Vector3> interLinesPoints = bbox.intersectionLinesWithPlane(plane);

            for (int ii = 0; ii < interLinesPoints.Count / 2; ++ii)
            {
                Debug.DrawRay(offset + interLinesPoints[2 * ii], interLinesPoints[2 * ii + 1] - interLinesPoints[2 * ii], Color.green);
            }
        }

        public static void displayBBoxGL(Material mat, DLL.BBox bbox, Vector3 offset)
        {
            GL.PushMatrix();

            GL.LoadOrtho();

            GL.Begin(GL.TRIANGLES);
            mat.SetPass(0);
            GL.Color(new Color(mat.color.r, mat.color.g, mat.color.b, mat.color.a));

            List<Vector3> linesPoints = bbox.linesPairPoints();

            for (int ii = 0; ii < linesPoints.Count; ++ii)
            {
                GL.Vertex(linesPoints[ii]);
            }
            //for (int ii = 0; ii < linesPoints.Count; ii += 2)
            //{
            //   
            //    
            //    
            //    Vector3 p1 = offset + linesPoints[ii];
            //    Vector3 p2 = linesPoints[ii + 1] - linesPoints[ii];
            //    GL.Vertex(p1);
            //    GL.Vertex(p2);
            //    GL.End();
            //}

            GL.Begin(GL.TRIANGLES);
            GL.Color(new Color(1, 1, 1, 1));
            GL.Vertex3(0.5F, 0.25F, 0);
            GL.Vertex3(0.25F, 0.25F, 0);
            GL.Vertex3(0.375F, 0.5F, 0);
            GL.End();


            GL.PopMatrix();
        }



        public static Mesh createTetrahedronMesh(float size)
        {
            Mesh mesh = new Mesh();

            Vector3 p0 = new Vector3(0, 0, 0);
            Vector3 p1 = new Vector3(size, 0, 0);
            Vector3 p2 = new Vector3(size * 0.5f, 0, Mathf.Sqrt(size * 0.75f));
            Vector3 p3 = new Vector3(size * 0.5f, Mathf.Sqrt(size * 0.75f), Mathf.Sqrt(size * 0.75f) / 3);

            mesh.vertices = new Vector3[]{
            p0,p1,p2,
            p0,p2,p3,
            p2,p1,p3,
            p0,p3,p1};

            mesh.triangles = new int[]{
            0,1,2,
            3,4,5,
            6,7,8,
            9,10,11};

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }


        public static Mesh createTube(float radius = 1.7f, float inter = 0.15f, float height = 0.1f, int nbSides = 60)
        {
            Mesh mesh = new Mesh();

            //float height = 0.2f;
            //int nbSides = 24;

            // Outter shell is at radius1 + radius2 / 2, inner shell at radius1 - radius2 / 2
            float bottomRadius1 = radius; // .5
            float bottomRadius2 = inter; // .15
            float topRadius1 = radius;
            float topRadius2 = inter;

            int nbVerticesCap = nbSides * 2 + 2;
            int nbVerticesSides = nbSides * 2 + 2;
            #region Vertices

            // bottom + top + sides
            Vector3[] vertices = new Vector3[nbVerticesCap * 2 + nbVerticesSides * 2];
            int vert = 0;
            float _2pi = Mathf.PI * 2f;

            // Bottom cap
            int sideCounter = 0;
            while (vert < nbVerticesCap)
            {
                sideCounter = sideCounter == nbSides ? 0 : sideCounter;

                float r1 = (float)(sideCounter++) / nbSides * _2pi;
                float cos = Mathf.Cos(r1);
                float sin = Mathf.Sin(r1);
                vertices[vert] = new Vector3(cos * (bottomRadius1 - bottomRadius2 * .5f), 0f, sin * (bottomRadius1 - bottomRadius2 * .5f));
                vertices[vert + 1] = new Vector3(cos * (bottomRadius1 + bottomRadius2 * .5f), 0f, sin * (bottomRadius1 + bottomRadius2 * .5f));
                vert += 2;
            }

            // Top cap
            sideCounter = 0;
            while (vert < nbVerticesCap * 2)
            {
                sideCounter = sideCounter == nbSides ? 0 : sideCounter;

                float r1 = (float)(sideCounter++) / nbSides * _2pi;
                float cos = Mathf.Cos(r1);
                float sin = Mathf.Sin(r1);
                vertices[vert] = new Vector3(cos * (topRadius1 - topRadius2 * .5f), height, sin * (topRadius1 - topRadius2 * .5f));
                vertices[vert + 1] = new Vector3(cos * (topRadius1 + topRadius2 * .5f), height, sin * (topRadius1 + topRadius2 * .5f));
                vert += 2;
            }

            // Sides (out)
            sideCounter = 0;
            while (vert < nbVerticesCap * 2 + nbVerticesSides)
            {
                sideCounter = sideCounter == nbSides ? 0 : sideCounter;

                float r1 = (float)(sideCounter++) / nbSides * _2pi;
                float cos = Mathf.Cos(r1);
                float sin = Mathf.Sin(r1);

                vertices[vert] = new Vector3(cos * (topRadius1 + topRadius2 * .5f), height, sin * (topRadius1 + topRadius2 * .5f));
                vertices[vert + 1] = new Vector3(cos * (bottomRadius1 + bottomRadius2 * .5f), 0, sin * (bottomRadius1 + bottomRadius2 * .5f));
                vert += 2;
            }

            // Sides (in)
            sideCounter = 0;
            while (vert < vertices.Length)
            {
                sideCounter = sideCounter == nbSides ? 0 : sideCounter;

                float r1 = (float)(sideCounter++) / nbSides * _2pi;
                float cos = Mathf.Cos(r1);
                float sin = Mathf.Sin(r1);

                vertices[vert] = new Vector3(cos * (topRadius1 - topRadius2 * .5f), height, sin * (topRadius1 - topRadius2 * .5f));
                vertices[vert + 1] = new Vector3(cos * (bottomRadius1 - bottomRadius2 * .5f), 0, sin * (bottomRadius1 - bottomRadius2 * .5f));
                vert += 2;
            }
            #endregion

            #region Normales

            // bottom + top + sides
            Vector3[] normales = new Vector3[vertices.Length];
            vert = 0;

            // Bottom cap
            while (vert < nbVerticesCap)
            {
                normales[vert++] = Vector3.down;
            }

            // Top cap
            while (vert < nbVerticesCap * 2)
            {
                normales[vert++] = Vector3.up;
            }

            // Sides (out)
            sideCounter = 0;
            while (vert < nbVerticesCap * 2 + nbVerticesSides)
            {
                sideCounter = sideCounter == nbSides ? 0 : sideCounter;

                float r1 = (float)(sideCounter++) / nbSides * _2pi;

                normales[vert] = new Vector3(Mathf.Cos(r1), 0f, Mathf.Sin(r1));
                normales[vert + 1] = normales[vert];
                vert += 2;
            }

            // Sides (in)
            sideCounter = 0;
            while (vert < vertices.Length)
            {
                sideCounter = sideCounter == nbSides ? 0 : sideCounter;

                float r1 = (float)(sideCounter++) / nbSides * _2pi;

                normales[vert] = -(new Vector3(Mathf.Cos(r1), 0f, Mathf.Sin(r1)));
                normales[vert + 1] = normales[vert];
                vert += 2;
            }
            #endregion

            #region UVs
            Vector2[] uvs = new Vector2[vertices.Length];

            vert = 0;
            // Bottom cap
            sideCounter = 0;
            while (vert < nbVerticesCap)
            {
                float t = (float)(sideCounter++) / nbSides;
                uvs[vert++] = new Vector2(0f, t);
                uvs[vert++] = new Vector2(1f, t);
            }

            // Top cap
            sideCounter = 0;
            while (vert < nbVerticesCap * 2)
            {
                float t = (float)(sideCounter++) / nbSides;
                uvs[vert++] = new Vector2(0f, t);
                uvs[vert++] = new Vector2(1f, t);
            }

            // Sides (out)
            sideCounter = 0;
            while (vert < nbVerticesCap * 2 + nbVerticesSides)
            {
                float t = (float)(sideCounter++) / nbSides;
                uvs[vert++] = new Vector2(t, 0f);
                uvs[vert++] = new Vector2(t, 1f);
            }

            // Sides (in)
            sideCounter = 0;
            while (vert < vertices.Length)
            {
                float t = (float)(sideCounter++) / nbSides;
                uvs[vert++] = new Vector2(t, 0f);
                uvs[vert++] = new Vector2(t, 1f);
            }
            #endregion

            #region Triangles
            int nbFace = nbSides * 4;
            int nbTriangles = nbFace * 2;
            int nbIndexes = nbTriangles * 3;
            int[] triangles = new int[nbIndexes];

            // Bottom cap
            int i = 0;
            sideCounter = 0;
            while (sideCounter < nbSides)
            {
                int current = sideCounter * 2;
                int next = sideCounter * 2 + 2;

                triangles[i++] = next + 1;
                triangles[i++] = next;
                triangles[i++] = current;

                triangles[i++] = current + 1;
                triangles[i++] = next + 1;
                triangles[i++] = current;

                sideCounter++;
            }

            // Top cap
            while (sideCounter < nbSides * 2)
            {
                int current = sideCounter * 2 + 2;
                int next = sideCounter * 2 + 4;

                triangles[i++] = current;
                triangles[i++] = next;
                triangles[i++] = next + 1;

                triangles[i++] = current;
                triangles[i++] = next + 1;
                triangles[i++] = current + 1;

                sideCounter++;
            }

            // Sides (out)
            while (sideCounter < nbSides * 3)
            {
                int current = sideCounter * 2 + 4;
                int next = sideCounter * 2 + 6;

                triangles[i++] = current;
                triangles[i++] = next;
                triangles[i++] = next + 1;

                triangles[i++] = current;
                triangles[i++] = next + 1;
                triangles[i++] = current + 1;

                sideCounter++;
            }


            // Sides (in)
            while (sideCounter < nbSides * 4)
            {
                int current = sideCounter * 2 + 6;
                int next = sideCounter * 2 + 8;

                triangles[i++] = next + 1;
                triangles[i++] = next;
                triangles[i++] = current;

                triangles[i++] = current + 1;
                triangles[i++] = next + 1;
                triangles[i++] = current;

                sideCounter++;
            }
            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();

            return mesh;
        }

    }

}