using System.Collections.Generic;
using UnityEngine;

namespace HBP.Module3D
{
    public class BrainMaterials
    {
        #region Properties
        /// <summary>
        /// Base material for the brain mesh
        /// </summary>
        private Material m_Brain;
        /// <summary>
        /// Transparent material for the brain mesh
        /// </summary>
        private Material m_TransparentBrain;
        /// <summary>
        /// Base material for the cut meshes
        /// </summary>
        private Material m_Cut;
        /// <summary>
        /// Transparent material for the cut meshes
        /// </summary>
        private Material m_TransparentCut;
        /// <summary>
        /// Is the material transparent ?
        /// </summary>
        public bool IsTransparent { get; set; }
        /// <summary>
        /// Currently used material for the brain
        /// </summary>
        public Material BrainMaterial
        {
            get
            {
                return IsTransparent ? m_TransparentBrain : m_Brain;
            }
        }
        /// <summary>
        /// Currently used material for the cuts
        /// </summary>
        public Material CutMaterial
        {
            get
            {
                return IsTransparent ? m_TransparentCut : m_Cut;
            }
        }
        #endregion

        #region Constructors
        public BrainMaterials()
        {
            m_Brain = Object.Instantiate(Resources.Load("Materials/Brain/Brain", typeof(Material))) as Material;
            m_TransparentBrain = Object.Instantiate(Resources.Load("Materials/Brain/TransparentBrain", typeof(Material))) as Material;
            m_Cut = Object.Instantiate(Resources.Load("Materials/Brain/Cut", typeof(Material))) as Material;
            m_TransparentCut = Object.Instantiate(Resources.Load("Materials/Brain/TransparentCut", typeof(Material))) as Material;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the texture used for the colormap
        /// </summary>
        /// <param name="colormap">Colormap texture to be used</param>
        public void SetBrainColormapTexture(Texture2D colormap)
        {
            m_Brain.SetTexture("_ColorTex", colormap);
            m_TransparentBrain.SetTexture("_ColorTex", colormap);
        }
        /// <summary>
        /// Set the texture used for the brain color
        /// </summary>
        /// <param name="color">Brain color texture to be used</param>
        public void SetBrainColorTexture(Texture2D color)
        {
            m_Brain.SetTexture("_MainTex", color);
            m_TransparentBrain.SetTexture("_MainTex", color);
        }
        /// <summary>
        /// Set the strong cuts parameter
        /// </summary>
        /// <param name="strongCuts">True if strong cuts are used</param>
        public void SetStrongCuts(bool strongCuts)
        {
            m_Brain.SetInt("_StrongCuts", strongCuts ? 1 : 0);
            m_TransparentBrain.SetInt("_StrongCuts", strongCuts ? 1 : 0);
        }
        /// <summary>
        /// Set the activity parameter
        /// </summary>
        /// <param name="activity">True if activity has to be displayed</param>
        public void SetActivity(bool activity)
        {
            m_Brain.SetInt("_Activity", activity ? 1 : 0);
            m_TransparentBrain.SetInt("_Activity", activity ? 1 : 0);
        }
        /// <summary>
        /// Set the cuts to the materials (to clip the vertices depending on the cuts)
        /// </summary>
        /// <param name="cuts">Cuts to be considered</param>
        public void SetCuts(List<Cut> cuts)
        {
            m_Brain.SetInt("_CutCount", cuts.Count);
            m_TransparentBrain.SetInt("_CutCount", cuts.Count);
            if (cuts.Count > 0)
            {
                List<Vector4> cutPoints = new List<Vector4>(20);
                for (int i = 0; i < 20; ++i)
                {
                    if (i < cuts.Count)
                    {
                        cutPoints.Add(new Vector4(-cuts[i].Point.x, cuts[i].Point.y, cuts[i].Point.z));
                    }
                    else
                    {
                        cutPoints.Add(Vector4.zero);
                    }
                }
                m_Brain.SetVectorArray("_CutPoints", cutPoints);
                m_TransparentBrain.SetVectorArray("_CutPoints", cutPoints);
                List<Vector4> cutNormals = new List<Vector4>(20);
                for (int i = 0; i < 20; ++i)
                {
                    if (i < cuts.Count)
                    {
                        cutNormals.Add(new Vector4(-cuts[i].Normal.x, cuts[i].Normal.y, cuts[i].Normal.z));
                    }
                    else
                    {
                        cutNormals.Add(Vector4.zero);
                    }
                }
                m_Brain.SetVectorArray("_CutNormals", cutNormals);
                m_TransparentBrain.SetVectorArray("_CutNormals", cutNormals);
            }
        }
        /// <summary>
        /// Set the center of the brain
        /// </summary>
        /// <param name="center">Center of the brain</param>
        public void SetBrainCenter(Vector3 center)
        {
            m_Brain.SetVector("_Center", center);
            m_TransparentBrain.SetVector("_Center", center);
        }
        /// <summary>
        /// Set the display atlas parameter
        /// </summary>
        /// <param name="displayAtlas">True if atlas is displayed</param>
        public void SetDisplayAtlas(bool displayAtlas)
        {
            m_Brain.SetInt("_Atlas", displayAtlas ? 1 : 0);
            m_TransparentBrain.SetInt("_Atlas", displayAtlas ? 1 : 0);
        }
        /// <summary>
        /// Set the alpha value of the transparent materials
        /// </summary>
        /// <param name="alpha">Alpha value to be used</param>
        public void SetAlpha(float alpha)
        {
            Color color = new Color(1, 1, 1, alpha);
            m_TransparentBrain.SetColor("_Color", color);
            m_TransparentCut.SetColor("_Color", color);
        }
        #endregion
    }
}